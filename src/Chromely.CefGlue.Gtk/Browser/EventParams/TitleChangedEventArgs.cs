﻿#region Port Info
/**
 * This is a port from CefGlue.WindowsForms sample of . Mostly provided as-is. 
 * For more info: https://bitbucket.org/xilium/xilium.cefglue/wiki/Home
 **/
#endregion

namespace Chromely.CefGlue.Gtk.Browser
{
    using System;

    public class TitleChangedEventArgs : EventArgs
	{
		public TitleChangedEventArgs(string title)
		{
			Title = title;
		}

		public string Title { get; private set; }
	}
}
