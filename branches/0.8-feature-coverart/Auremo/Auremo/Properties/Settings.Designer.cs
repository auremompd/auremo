﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18051
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Auremo.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool EnableVolumeControl {
            get {
                return ((bool)(this["EnableVolumeControl"]));
            }
            set {
                this["EnableVolumeControl"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("6600")]
        public int Port {
            get {
                return ((int)(this["Port"]));
            }
            set {
                this["Port"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int ReconnectInterval {
            get {
                return ((int)(this["ReconnectInterval"]));
            }
            set {
                this["ReconnectInterval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("localhost")]
        public string Server {
            get {
                return ((string)(this["Server"]));
            }
            set {
                this["Server"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public int ViewUpdateInterval {
            get {
                return ((int)(this["ViewUpdateInterval"]));
            }
            set {
                this["ViewUpdateInterval"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string Password {
            get {
                return ((string)(this["Password"]));
            }
            set {
                this["Password"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int VolumeAdjustmentStep {
            get {
                return ((int)(this["VolumeAdjustmentStep"]));
            }
            set {
                this["VolumeAdjustmentStep"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int MouseWheelAdjustsSongPositionPercentBy {
            get {
                return ((int)(this["MouseWheelAdjustsSongPositionPercentBy"]));
            }
            set {
                this["MouseWheelAdjustsSongPositionPercentBy"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int MouseWheelAdjustsSongPositionSecondsBy {
            get {
                return ((int)(this["MouseWheelAdjustsSongPositionSecondsBy"]));
            }
            set {
                this["MouseWheelAdjustsSongPositionSecondsBy"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool MouseWheelAdjustsSongPositionInPercent {
            get {
                return ((bool)(this["MouseWheelAdjustsSongPositionInPercent"]));
            }
            set {
                this["MouseWheelAdjustsSongPositionInPercent"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool InitialSetupDone {
            get {
                return ((bool)(this["InitialSetupDone"]));
            }
            set {
                this["InitialSetupDone"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ByDate")]
        public string AlbumSortingMode {
            get {
                return ((string)(this["AlbumSortingMode"]));
            }
            set {
                this["AlbumSortingMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>YYYY</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection AlbumDateFormats {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["AlbumDateFormats"]));
            }
            set {
                this["AlbumDateFormats"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool QuickSearchTabIsVisible {
            get {
                return ((bool)(this["QuickSearchTabIsVisible"]));
            }
            set {
                this["QuickSearchTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ArtistListTabIsVisible {
            get {
                return ((bool)(this["ArtistListTabIsVisible"]));
            }
            set {
                this["ArtistListTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ArtistTreeTabIsVisible {
            get {
                return ((bool)(this["ArtistTreeTabIsVisible"]));
            }
            set {
                this["ArtistTreeTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool GenreListTabIsVisible {
            get {
                return ((bool)(this["GenreListTabIsVisible"]));
            }
            set {
                this["GenreListTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool GenreTreeTabIsVisible {
            get {
                return ((bool)(this["GenreTreeTabIsVisible"]));
            }
            set {
                this["GenreTreeTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FilesystemTabIsVisible {
            get {
                return ((bool)(this["FilesystemTabIsVisible"]));
            }
            set {
                this["FilesystemTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool StreamsTabIsVisible {
            get {
                return ((bool)(this["StreamsTabIsVisible"]));
            }
            set {
                this["StreamsTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool PlaylistsTabIsVisible {
            get {
                return ((bool)(this["PlaylistsTabIsVisible"]));
            }
            set {
                this["PlaylistsTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SearchTab")]
        public string DefaultMusicCollectionTab {
            get {
                return ((string)(this["DefaultMusicCollectionTab"]));
            }
            set {
                this["DefaultMusicCollectionTab"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Append")]
        public string SendToPlaylistMethod {
            get {
                return ((string)(this["SendToPlaylistMethod"]));
            }
            set {
                this["SendToPlaylistMethod"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int NetworkTimeout {
            get {
                return ((int)(this["NetworkTimeout"]));
            }
            set {
                this["NetworkTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool AdvancedTabIsVisible {
            get {
                return ((bool)(this["AdvancedTabIsVisible"]));
            }
            set {
                this["AdvancedTabIsVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UseAlbumArtist {
            get {
                return ((bool)(this["UseAlbumArtist"]));
            }
            set {
                this["UseAlbumArtist"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int WindowX {
            get {
                return ((int)(this["WindowX"]));
            }
            set {
                this["WindowX"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int WindowY {
            get {
                return ((int)(this["WindowY"]));
            }
            set {
                this["WindowY"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("960")]
        public int WindowW {
            get {
                return ((int)(this["WindowW"]));
            }
            set {
                this["WindowW"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("590")]
        public int WindowH {
            get {
                return ((int)(this["WindowH"]));
            }
            set {
                this["WindowH"] = value;
            }
        }
    }
}
