﻿#region Port Info
/**
 * This is a port from CefGlue.WindowsForms sample of . Mostly provided as-is. 
 * For more info: https://bitbucket.org/xilium/xilium.cefglue/wiki/Home
 **/
#endregion

namespace Chromely.CefGlue.Gtk.Browser
{
    using System;

    public class PluginCrashedEventArgs : EventArgs
	{
		public PluginCrashedEventArgs(string pluginPath)
		{
			PluginPath = pluginPath;
		}

		public string PluginPath { get; private set; }
	}
}
