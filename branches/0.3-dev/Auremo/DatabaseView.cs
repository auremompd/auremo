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

        public DatabaseView(Database database)
        {
            m_Database = database;

            AlbumsOfSelectedGenres = new ObservableCollection<AlbumMetadata>();
            SongsOnSelectedAlbumsOfSelectedGenres = new ObservableCollection<SongMetadata>();
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

        public void OnSelectedGenresChanged(IList selection)
        {
            AlbumsOfSelectedGenres.Clear();
            ISet<string> sortedGenres = new SortedSet<string>();

            foreach (object o in selection)
            {
                sortedGenres.Add(o as string);
            }

            foreach (string genre in sortedGenres)
            {
                foreach (AlbumMetadata album in m_Database.AlbumsByGenre(genre))
                {
                    AlbumsOfSelectedGenres.Add(album);
                }
            }
        }

        public void OnSelectedAlbumsOfSelectedGenresChanged(IList selection)
        {
            SongsOnSelectedAlbumsOfSelectedGenres.Clear();
            ISet<AlbumMetadata> sortedAlbums = new SortedSet<AlbumMetadata>();

            foreach (object o in selection)
            {
                sortedAlbums.Add(o as AlbumMetadata);
            }

            foreach (AlbumMetadata album in sortedAlbums)
            {
                foreach (SongMetadata song in m_Database.Songs(album))
                {
                    SongsOnSelectedAlbumsOfSelectedGenres.Add(song);
                }
            }
        }
    }
}
