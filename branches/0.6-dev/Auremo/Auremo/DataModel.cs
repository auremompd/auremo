﻿/*
 * Copyright 2013 Mikko Teräs and Niilo Säämänen.
 *
 * This file is part of Auremo.
 *
 * Auremo is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, version 2.
 *
 * Auremo is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Auremo. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class DataModel
    {
        public DataModel()
        {
            ServerConnection = new ServerConnection();
            ServerStatus = new ServerStatus();
            Database = new Database(this);
            CollectionSearch = new CollectionSearch(this);
            DatabaseView = new DatabaseView(this);
            StreamsCollection = new StreamsCollection();
            SavedPlaylists = new SavedPlaylists();
            Playlist = new Playlist(this);
            OutputCollection = new OutputCollection();
        }

        public ServerConnection ServerConnection
        {
            get;
            private set;
        }

        public ServerStatus ServerStatus
        {
            get;
            private set;
        }

        public Database Database
        {
            get;
            private set;
        }

        public CollectionSearch CollectionSearch
        {
            get;
            private set;
        }

        public DatabaseView DatabaseView
        {
            get;
            private set;
        }

        public StreamsCollection StreamsCollection
        {
            get;
            private set;
        }

        public SavedPlaylists SavedPlaylists
        {
            get;
            private set;
        }

        public Playlist Playlist
        {
            get;
            private set;
        }

        public OutputCollection OutputCollection
        {
            get;
            private set;
        }
    }
}