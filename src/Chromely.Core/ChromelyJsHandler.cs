﻿/**
 MIT License

 Copyright (c) 2017 Kola Oyewumi

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all
 copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
 */
 
namespace Chromely.Core.Infrastructure
{
    using System;

    public class ChromelyJsHandler
    {
        public ChromelyJsHandler()
        {
            Key = Guid.NewGuid().ToString();
        }

        public ChromelyJsHandler(string objectNameToBind, bool registerAsync)
        {
            Key = Guid.NewGuid().ToString();
            ObjectNameToBind = objectNameToBind;
            BoundObject = null;
            RegisterAsAsync = registerAsync;
            BindingOptions = null;
            UseDefault = true;
        }

        public ChromelyJsHandler(string objectNameToBind, object boundObject, object bindingOptions, bool registerAsync)
        {
            Key = Guid.NewGuid().ToString();
            ObjectNameToBind = objectNameToBind;
            BoundObject = boundObject;
            RegisterAsAsync = registerAsync;
            BindingOptions = bindingOptions;
            UseDefault = false;
        }

        public string Key { get; private set; }
        public string ObjectNameToBind { get; set; }
        public object BoundObject { get; set; }
        public object BindingOptions { get; set; }
        public bool RegisterAsAsync { get; set; }
        public bool UseDefault { get; set; }
    }
}
