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

namespace Chromely.CefGlue.Gtk.ChromeHost
{
    using System;
    using Xilium.CefGlue.Wrapper;
    using Xilium.CefGlue;
    using Chromely.CefGlue.Gtk.Browser;
    using System.Reflection;
    using System.Collections.Generic;
    using Chromely.Core;
    using Chromely.Core.Infrastructure;
    using System.IO;
    using Chromely.Core.RestfulService;
    using System.Linq;
    using Chromely.CefGlue.Gtk.Browser.Handlers;

    public abstract class HostBase : IChromelyServiceProvider, IDisposable
    {
        private Window m_mainView;

        public HostBase(ChromelyConfiguration hostConfig)
        {
            HostConfig = hostConfig;
            ServiceAssemblies = new List<Assembly>();
        }

        public static CefMessageRouterBrowserSide BrowserMessageRouter { get; private set; }

        public ChromelyConfiguration HostConfig { get; private set; }

        public List<Assembly> ServiceAssemblies { get; private set; }

        protected Window MainView { get { return m_mainView; } }

        public int Run(string[] args)
        {
            try
            {
                return RunInternal(args);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                return 1;
            }
        }

        private int RunInternal(string[] args)
        {
            CefRuntime.Load();

            var settings = new CefSettings();
            settings.MultiThreadedMessageLoop = true;
            settings.SingleProcess = false;
            settings.LogSeverity = CefLogSeverity.Verbose;
            settings.LogFile = HostConfig.LogFile;
            settings.ResourcesDirPath = Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath);
            settings.LocalesDirPath = Path.Combine(settings.ResourcesDirPath, "locales");
            settings.RemoteDebuggingPort = 20480;
            settings.NoSandbox = true;
            settings.Locale = HostConfig.Locale;

            var argv = args;
            if (CefRuntime.Platform != CefRuntimePlatform.Windows)
            {
                argv = new string[args.Length + 1];
                Array.Copy(args, 0, argv, 1, args.Length);
                argv[0] = "-";
            }

            // Update configuration settings
            settings.Update(HostConfig.CustomSettings);

            var mainArgs = new CefMainArgs(argv);
            var app = new CefGlueApp(HostConfig);

            var exitCode = CefRuntime.ExecuteProcess(mainArgs, app, IntPtr.Zero);
            Log.Info((string.Format("CefRuntime.ExecuteProcess() returns {0}", exitCode)));

            if (exitCode != -1)
            {
                // An error has occured.
                return exitCode;
            }

            // guard if something wrong
            foreach (var arg in args) { if (arg.StartsWith("--type=")) { return -2; } }

            CefRuntime.Initialize(mainArgs, settings, app, IntPtr.Zero);

            RegisterSchemeHandlers();
            RegisterMessageRouters();

            PlatformInitialize();

            m_mainView = CreateMainView();

            PlatformRunMessageLoop();

            m_mainView.Dispose();
            m_mainView = null;

            CefRuntime.Shutdown();

            PlatformShutdown();
            return 0;
        }

        public void Quit()
        {
            PlatformQuitMessageLoop();
        }

        protected abstract void PlatformInitialize();

        protected abstract void PlatformShutdown();

        protected abstract void PlatformRunMessageLoop();

        protected abstract void PlatformQuitMessageLoop();

        protected abstract Window CreateMainView();


        public void RegisterUrlScheme(UrlScheme scheme)
        {
            UrlSchemeProvider.RegisterScheme(scheme);
        }

        public void RegisterServiceAssembly(string filename)
        {
            ServiceAssemblies?.RegisterServiceAssembly(Assembly.LoadFile(filename));
        }

        public void RegisterServiceAssembly(Assembly assembly)
        {
            ServiceAssemblies?.RegisterServiceAssembly(assembly);
        }

        public void RegisterServiceAssemblies(string folder)
        {
            ServiceAssemblies?.RegisterServiceAssemblies(folder);
        }

        public void RegisterServiceAssemblies(List<string> filenames)
        {
            ServiceAssemblies?.RegisterServiceAssemblies(filenames);
        }

        public void ScanAssemblies()
        {
            if ((ServiceAssemblies == null) || (ServiceAssemblies.Count == 0))
            {
                return;
            }

            foreach (var assembly in ServiceAssemblies)
            {
                RouteScanner scanner = new RouteScanner(assembly);
                Dictionary<string, Route> currentRouteDictionary = scanner.Scan();
                ServiceRouteProvider.MergeRoutes(currentRouteDictionary);
            }
        }

        private void RegisterSchemeHandlers()
        {
            // Register scheme handlers
            object[] schemeHandlerObjs = IoC.GetAllInstances(typeof(ChromelySchemeHandler));
            if (schemeHandlerObjs != null)
            {
                var schemeHandlers = schemeHandlerObjs.ToList();

                foreach (var item in schemeHandlers)
                {
                    if (item is ChromelySchemeHandler)
                    {
                        ChromelySchemeHandler handler = (ChromelySchemeHandler)item;
                        if (handler.HandlerFactory == null)
                        {
                            if (handler.UseDefaultResource)
                            {
                                CefRuntime.RegisterSchemeHandlerFactory(handler.SchemeName, handler.DomainName, new CefGlueResourceSchemeHandlerFactory());
                            }

                            if (handler.UseDefaultHttp)
                            {
                                CefRuntime.RegisterSchemeHandlerFactory(handler.SchemeName, handler.DomainName, new CefGlueHttpSchemeHandlerFactory());
                            }
                        }
                        else if (handler.HandlerFactory is CefSchemeHandlerFactory)
                        {
                            CefRuntime.RegisterSchemeHandlerFactory(handler.SchemeName, handler.DomainName, (CefSchemeHandlerFactory)handler.HandlerFactory);
                        }
                    }
                }
            }
        }

        private void RegisterMessageRouters()
        {
            if (!CefRuntime.CurrentlyOn(CefThreadId.UI))
            {
                PostTask(CefThreadId.UI, this.RegisterMessageRouters);
                return;
            }

            BrowserMessageRouter = new CefMessageRouterBrowserSide(new CefMessageRouterConfig());

            // Register message router handlers
            List<object> messageRouterHandlers = IoC.GetAllInstances(typeof(ChromelyMesssageRouter)).ToList();
            if ((messageRouterHandlers != null) && (messageRouterHandlers.Count > 0))
            {
                var routerHandlers = messageRouterHandlers.ToList();

                foreach (var item in routerHandlers)
                {
                    ChromelyMesssageRouter routerHandler = (ChromelyMesssageRouter)item;
                    if (routerHandler.Handler is CefMessageRouterBrowserSide.Handler)
                    {
                        BrowserMessageRouter.AddHandler((CefMessageRouterBrowserSide.Handler)routerHandler.Handler);
                    }
                }
            }
            else
            {
                BrowserMessageRouter.AddHandler(new CefGlueMessageRouterHandler());
            }
        }

        public static void PostTask(CefThreadId threadId, Action action)
        {
            CefRuntime.PostTask(threadId, new ActionTask(action));
        }

        internal sealed class ActionTask : CefTask
        {
            public Action m_action;

            public ActionTask(Action action)
            {
                m_action = action;
            }

            protected override void Execute()
            {
                m_action();
                m_action = null;
            }
        }

        public delegate void Action();

        #region IDisposable

        ~HostBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion
    }
}
