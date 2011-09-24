﻿// <copyright file="SelectElement.cs" author="Brandon Stirnaman">
//     Copyright (c) 2011 Brandon Stirnaman, All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAutomation.API.Interfaces;
using Automation = global::WatiN;
using FluentAutomation.API.Enumerations;
using FluentAutomation.API;

namespace FluentAutomation.WatiN
{
    class SelectElement : Element, ISelectElement
    {
        private Automation.Core.SelectList _element = null;

        public SelectElement(Automation.Core.SelectList element) : base(element)
        {
            _element = element;
        }

        public bool IsMultiple
        {
            get
            {
                return _element.Multiple;
            }
        }

        public override string GetValue()
        {
            return _element.SelectedOption.Value;
        }

        public string GetOptionText()
        {
            return _element.SelectedOption.Text;
        }

        public string[] GetValues()
        {
            return _element.Options.Where(o => o.Selected).Select(o => o.Value).ToArray();
        }

        public string[] GetOptionValues()
        {
            return _element.Options.Select(o => o.Value).ToArray();
        }

        public string[] GetOptionsText()
        {
            return _element.Options.Select(o => o.Text).ToArray();
        }

        public int GetSelectedIndex()
        {
            return _element.SelectedOption.Index;
        }

        public int[] GetSelectedIndices()
        {
            return _element.Options.Where(o => o.Selected).Select(o => o.Index).ToArray();
        }

        public override void SetValue(string value)
        {
            SetValue(value, SelectMode.Value);
        }

        public void SetValue(string value, SelectMode selectMode)
        {
            if (selectMode == SelectMode.Value)
            {
                _element.SelectByValue(value);
            }
            else if (selectMode == SelectMode.Text)
            {
                _element.Select(value);
            }
            else if (selectMode == SelectMode.Index)
            {
                _element.Options[Int32.Parse(value)].Select();
            }

            this.OnChange();
        }

        public void SetValues(string[] values, SelectMode selectMode)
        {
            foreach (var value in values)
            {
                SetValue(value, selectMode);
            }

            if (_element.SelectedOptions.Count == 0)
            {
                if (selectMode == SelectMode.Value)
                    throw new SelectException("Selection failed. No option values matched collection provided.");
                else if (selectMode == SelectMode.Text)
                    throw new SelectException("Selection failed. No options text matched collection provided.");
                else if (selectMode == SelectMode.Index)
                    throw new SelectException("Selection failed. No options matched collection of indices provided.");
            }

            this.OnChange();
        }

        public void SetValues(Func<string, bool> optionMatchingFunc, SelectMode selectMode)
        {
            IEnumerable<Automation.Core.Option> options = null;

            if (selectMode == SelectMode.Text)
            {
                options = _element.Options.Where(x => optionMatchingFunc(x.Text));
            }
            else if (selectMode == SelectMode.Value)
            {
                options = _element.Options.Where(x => optionMatchingFunc(x.Value));
            }

            if (options != null)
            {
                foreach (var option in options)
                {
                    option.Select();
                }

                if (options.Count() == 0)
                {
                    if (selectMode == SelectMode.Value)
                        throw new SelectException("Selection failed. No option values matched expression [{0}] on element.", optionMatchingFunc);
                    else if (selectMode == SelectMode.Text)
                        throw new SelectException("Selection failed. No option text matched expression [{0}] on element.", optionMatchingFunc);
                    else if (selectMode == SelectMode.Index)
                        throw new SelectException("Selection failed. No options matched collection of indices provided.");
                }

                this.OnChange();
            }
        }

        public void SetSelectedIndex(int selectedIndex)
        {
            SetValue(selectedIndex.ToString(), SelectMode.Index);
            this.OnChange();
        }

        public void SetSelectedIndices(int[] selectedIndices)
        {
            foreach (var selectedIndex in selectedIndices)
            {
                SetSelectedIndex(selectedIndex);
            }

            this.OnChange();
        }

        public void ClearSelectedItems()
        {
            _element.ClearList();
        }
    }
}
