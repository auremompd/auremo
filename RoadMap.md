# Roadmap #

This is a partial list of features that are planned for future releases but are not yet in any development branch.

There is no absolute guarantee that features will appear in the exact version under which they are listed, or at all. Features which are difficult or time-consuming to implement may be pushed to later versions without notice if they are hindering the release of an otherwise publishable version.

Suggestions are welcome! None of these features or software versions are being developed yet, so if you have ideas about how they should work, or would like to see something added to the lists, there is still time to let me know. Please [e-mail](mailto:auremompd@gmail.com) the author.

## Features to be added in the near future ##

  * Add Mopidy support.
  * Use MPD command lists for playlist control for speed and atomicity.
  * Add filesystem album ordering.
  * Add support for choosing between artist and album artist when parsing the collection.
  * Make network communication asynchronous. Rewrite the protocol parser in a more orthodox way.
  * Remember window size, position and splitter states between restarts.
  * Add new columns to various data views. Audio quality, track number, album and song year, etc. Add options for hiding unwanted columns.
  * Add "view in artist/genre/filesystem tab" commands in the playlist's context menu.
  * Add a song duration column to the playlist view and show a total playlist duration somewhere.
  * Add "all artists", "all albums" etc items in the relevant music collection views.
  * Add multi-language support. If you are a native speaker of a language for which you would be willing to provide a translation, please let us know. Translators Italian, Portuguese and German have already signed up.

## Features to be added in the mid-term future ##

  * Optimize the initial database fetch. There are user reports of very long startup times with slow computers and large collections (20+ seconds with a Raspberry Pi as the server). Caching all the metadata on the client should be simple and help enormously.
  * Add a mini-player view.

## Features that might get added sooner or later, but no promises ##

  * Make media keys work even when Auremo is not focused. (It turns out .NET does not support functionality like this and all the solutions I've been able to find look far too evil to actually implement. Users who want this feature are encouraged to do more research on the topic and, if possible, send patches.)
  * Add support for selecting a color scheme (if feasible to implement in XAML).
  * Don't forget user settings whenever the software version, executable location, IP address etc change -- if reasonably possible.
  * Look into the possibility of supporting multiple genres per song.
  * Maybe create a cool custom control for the volume knob. The slider is typical but not very pretty, plus it takes a lot of space.
  * Maybe add album art, supposing that it can be incorporated in the GUI in a meaningful way. This should be an opt-in feature because it can be heavy and not useful for all users.
  * Maybe add lyrics. Also an opt-in feature.
  * Maybe add support for playing back the HTTP stream from the MPD server.