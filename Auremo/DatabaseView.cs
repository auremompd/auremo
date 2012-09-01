using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Auremo
{
    public class DatabaseView
    {
        private Database m_Database = null;

        public delegate ISet<AlbumMetadata> AlbumsUnderRoot(string root);
        public delegate ISet<SongMetadata> SongsOnAlbum(AlbumMetadata album);

        public DatabaseView(Database database)
        {
            m_Database = database;

            AlbumsBySelectedArtists = new ObservableCollection<AlbumMetadata>();
            SongsOnSelectedAlbumsBySelectedArtists = new ObservableCollection<SongMetadata>();
            AlbumsOfSelectedGenres = new ObservableCollection<AlbumMetadata>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<SongMetadata>();
        }

        public IList<string> Artists
        {
            get
            {
                return m_Database.Artists;
            }
        }

        public IList<AlbumMetadata> AlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public IList<SongMetadata> SongsOnSelectedAlbumsBySelectedArtists
        {
            get;
            private set;
        }

        public IList<string> Genres
        {
            get
            {
                return m_Database.Genres;
            }
        }

        public IList<AlbumMetadata> AlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public IList<SongMetadata> SongsOnSelectedAlbumsOfSelectedGenres
        {
            get;
            private set;
        }

        public void OnSelectedArtistsChanged(IList selection)
        {
            OnRootLevelSelectionChanged(selection, AlbumsBySelectedArtists, m_Database.AlbumsByArtist);
        }

        public void OnSelectedAlbumsBySelectedArtistsChanged(IList selection)
        {
            OnAlbumLevelSelectionChanged(selection, SongsOnSelectedAlbumsBySelectedArtists, m_Database.SongsByAlbum);
        }

        public void OnSelectedGenresChanged(IList selection)
        {
            OnRootLevelSelectionChanged(selection, AlbumsOfSelectedGenres, m_Database.AlbumsByGenre);
        }

        public void OnSelectedAlbumsOfSelectedGenresChanged(IList selection)
        {
            OnAlbumLevelSelectionChanged(selection, SongsOnSelectedAlbumsOfSelectedGenres, m_Database.SongsByAlbum);
        }

        private void OnRootLevelSelectionChanged(IList newSelection, IList<AlbumMetadata> albumView, AlbumsUnderRoot Albums)
        {
            albumView.Clear();
            ISet<string> sortedItems = new SortedSet<string>();

            foreach (object o in newSelection)
            {
                sortedItems.Add(o as string);
            }

            foreach (string item in sortedItems)
            {
                foreach (AlbumMetadata album in Albums(item))
                {
                    albumView.Add(album);
                }
            }
        }

        public void OnAlbumLevelSelectionChanged(IList newSelection, IList<SongMetadata> songView, SongsOnAlbum Songs)
        {
            songView.Clear();
            ISet<AlbumMetadata> sortedAlbums = new SortedSet<AlbumMetadata>();

            foreach (object o in newSelection)
            {
                sortedAlbums.Add(o as AlbumMetadata);
            }

            foreach (AlbumMetadata album in sortedAlbums)
            {
                foreach (SongMetadata song in Songs(album))
                {
                    songView.Add(song);
                }
            }
        }
    }
}
