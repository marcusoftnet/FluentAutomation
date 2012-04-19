﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Fleck;
using FluentAutomation.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FluentAutomation
{
    public class CommandProvider : BaseCommandProvider, ICommandProvider, IDisposable
    {
        private readonly IWebSocketServer phantomWebSocket = null;
        private readonly int portNumber = -1;
        private readonly Process phantomProcess = null;

        private IWebSocketConnection phantomConnection = null;
        private volatile bool isPhantomReady = false;

        public CommandProvider()
        {
            this.portNumber = this.getRandomUnusedPort();
            this.phantomWebSocket = new WebSocketServer(string.Format("ws://0.0.0.0:{0}", this.portNumber));
            this.OpenPhantomWebSocket();

            this.phantomProcess = this.startPhantomJS(this.portNumber);
            this.waitForPhantomReady();
        }

        private void OpenPhantomWebSocket()
        {
            Console.WriteLine("Opening communication with PhantomJS on port " + this.portNumber);
            this.phantomWebSocket.Start((socket) =>
            {
                socket.OnOpen = () =>
                {
                    this.phantomConnection = socket;
                    this.isPhantomReady = true;
                };
                socket.OnClose = () => {
                    this.phantomConnection = null;
                    this.waitForPhantomReady();
                };
                socket.OnMessage = (message) =>
                {
                    var messageData = JObject.Parse(message);
                    if (messageData["Response"] != null)
                    {
                        this.isPhantomReady = true;
                    }
                };
            });
        }

        private void waitForPhantomReady()
        {
            this.isPhantomReady = false;
            while (this.isPhantomReady == false)
            {
            }
        }
        
        private Process startPhantomJS(int portNumber)
        {
            var filePath = "phantomjs.exe";
            var workingDirectory = UnpackResources();

            ProcessStartInfo startInfo = new ProcessStartInfo(filePath);
            startInfo.WorkingDirectory = Path.GetDirectoryName(workingDirectory);
            startInfo.CreateNoWindow = true;
            startInfo.Arguments = "PhantomWebSocketServer.coffee " + portNumber.ToString();
            return Process.Start(startInfo);
        }

        private static string UnpackResources()
        {
            var unpackResource = new Action<string, Assembly>((resourceFileName, assembly) =>
            {
                var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(resourceFileName));
                if (!File.Exists(resourceFileName))
                {
                    var resourceStream = assembly.GetManifestResourceStream(resourceName);
                    var resourceBytes = new byte[(int)resourceStream.Length];

                    resourceStream.Read(resourceBytes, 0, resourceBytes.Length);
                    File.WriteAllBytes(resourceFileName, resourceBytes);
                }
            });

            var containerAssembly = Assembly.GetAssembly(typeof(PhantomJS));

            unpackResource("phantomjs.exe", containerAssembly);
            unpackResource("PhantomWebSocketServer.coffee", containerAssembly);

            return containerAssembly.CodeBase;
        }
        
        private int getRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = (listener.LocalEndpoint as IPEndPoint).Port;
            listener.Stop();

            return port;
        }

        public void Navigate(Uri url)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Navigate", Url = url.ToString() }));
            this.waitForPhantomReady();
        }

        public Func<IElement> Find(string selector)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Find", Selector = selector }));
            this.waitForPhantomReady();
            return () => new Element(selector);
        }

        public Func<IEnumerable<IElement>> FindMultiple(string selector)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "FindMultiple", Selector = selector }));
            this.waitForPhantomReady();
            return () => new List<IElement>() { new Element(selector) };
        }

        public void Click(int x, int y)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Click", Selector = "", X = x, Y = y }));
            this.waitForPhantomReady();
        }

        public void Click(Func<IElement> element, int x, int y)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Click", Selector = element().Selector, X = x, Y = y }));
            this.waitForPhantomReady();
        }

        public void Click(Func<IElement> element)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Click", Selector = element().Selector, X = 0, Y = 0 }));
            this.waitForPhantomReady();
        }

        public void Hover(int x, int y)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Hover", Selector = "", X = x, Y = y }));
            this.waitForPhantomReady();
        }

        public void Hover(Func<IElement> element, int x, int y)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Hover", Selector = element().Selector, X = x, Y = y }));
            this.waitForPhantomReady();
        }

        public void Hover(Func<IElement> element)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Hover", Selector = element().Selector, X = 0, Y = 0 }));
            this.waitForPhantomReady();
        }

        public void Focus(Func<IElement> element)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Focus", Selector = element().Selector, X = 0, Y = 0 }));
            this.waitForPhantomReady();
        }

        public void DragAndDrop(Func<IElement> source, Func<IElement> target)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "DragAndDrop", SourceSelector = source().Selector, TargetSelector = target().Selector }));
            this.waitForPhantomReady();
        }

        public void EnterText(Func<IElement> element, string text)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "EnterText", Selector = element().Selector, Text = text }));
            this.waitForPhantomReady();
        }

        public void SelectText(Func<IElement> element, string optionText)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "SelectText", Selector = element().Selector, Text = optionText }));
            this.waitForPhantomReady();
        }

        public void SelectValue(Func<IElement> element, string optionValue)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "SelectValue", Selector = element().Selector, Value = optionValue }));
            this.waitForPhantomReady();
        }

        public void SelectIndex(Func<IElement> element, int optionIndex)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "SelectIndex", Selector = element().Selector, Index = optionIndex }));
            this.waitForPhantomReady();
        }

        public void MultiSelectText(Func<IElement> element, string[] optionTextCollection)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "MultiSelectText", Selector = element().Selector, Text = optionTextCollection }));
            this.waitForPhantomReady();
        }

        public void MultiSelectValue(Func<IElement> element, string[] optionValues)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "MultiSelectValue", Selector = element().Selector, Value = optionValues }));
            this.waitForPhantomReady();
        }

        public void MultiSelectIndex(Func<IElement> element, int[] optionIndices)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "MultiSelectIndex", Selector = element().Selector, Index = optionIndices }));
            this.waitForPhantomReady();
        }

        public void TakeScreenshot(string screenshotName)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "TakeScreenshot", FileName = screenshotName }));
            this.waitForPhantomReady();
        }

        public void UploadFile(Func<IElement> element, int x, int y, string fileName)
        {
            throw new NotImplementedException();
        }

        public void Wait(int seconds)
        {
            this.Wait(TimeSpan.FromSeconds(seconds));
        }

        public void Wait()
        {
            this.Wait(Settings.DefaultWaitTimeout);
        }

        public void Wait(TimeSpan timeSpan)
        {
            this.Act(() => System.Threading.Thread.Sleep(timeSpan));
        }

        public void WaitUntil(System.Linq.Expressions.Expression<Func<bool>> conditionFunc)
        {
            throw new NotImplementedException();
        }

        public void WaitUntil(System.Linq.Expressions.Expression<Func<bool>> conditionFunc, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void WaitUntil(System.Linq.Expressions.Expression<Action> conditionAction)
        {
            throw new NotImplementedException();
        }

        public void WaitUntil(System.Linq.Expressions.Expression<Action> conditionAction, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void Press(string keys)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Press", Keys = keys }));
            this.waitForPhantomReady();
        }

        public void Type(string text)
        {
            this.phantomConnection.Send(JsonConvert.SerializeObject(new { Action = "Type", Text = text }));
            this.waitForPhantomReady();
        }

        public void Dispose()
        {
            try
            {
                this.phantomWebSocket.Dispose();
                this.phantomProcess.Kill();
            }
            catch (Exception) { }
        }
    }
}