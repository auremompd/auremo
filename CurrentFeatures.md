# Auremo features #

Version 0.6.1 supports the following features.

  * Connection to a local or remote MPD server.
    * Password protected servers are supported.
      * The number of passwords the client can send is limited to one.
      * The password is saved in the user settings in encrypted form. However, it is sent to the server as plaintext as required by the MPD protocol.
    * Automatically reconnect to the server after a configurable time when connection to the server has been lost.
  * Music database browsing.
    * The music collection can be viewed in multiple different ways:
      * Artists, albums and song can be searched for on the search tab.
      * Lists of artists, the albums by selected artists and songs on the selected albums.
      * A tree view of artists, albums and songs.
      * Lists of music genres, albums belonging to selected genres and songs on the selected albums.
      * A tree view of genres, albums and songs.
      * A tree view of the file system containing the music collection.
  * HTTP streams (e.g., web radio channels) are supported.
    * Streams can added by providing a URL and a name, or loaded from M3U or PLS playlist files.
    * Stream addresses are saved on the local computer and appear as a part of the music collection. They can be added to the playlist just like songs.
  * Playlist construction, manipulation, loading and saving.
    * Elements (individual songs, albums, directories etc) in the music can be sent to the playlist.
    * Songs on the playlist can be reordered or removed.
    * The playlist can be saved on the server. Saved playlists appear under the playlists tab.
    * Saved playlists on the server can be loaded for use.
    * Saved playlists on the server can be renamed or deleted.
    * Individual tracks on a saved playlist can be sent to the current playlist.
  * Playback control.
    * Skip, play, pause, stop and next buttons.
    * Media keys on some keyboards (play/pause, stop, next, previous, volume up/down) are supported.
    * Random and repeat buttons.
    * Songs on the playlist can be jumped to directly with a double-click or pressing Enter.
    * A seek bar allows the user to skip back or ahead in a song.
  * Customizable user interface.
    * Unused music collection views can be hidden and the default one to use can be selected.
    * Customizable quick select behavior: double-clicking or pressing the Enter key in the music collection can be set to do any of the following:
      * Add the selected items to the end of the current playlist.
      * Add the selected items after the current track on the current playlist.
      * Clear the current playlist and start playing the selected items as the new playlist.


---


More features are planned. The UpcomingFeatures page lists changes being implemented for the next release.