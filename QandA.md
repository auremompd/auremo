# Questions and Answers #

Here are some answers to a few obvious questions you might have. If you have more, please e-mail the author.

### Why did I lose all my streams library when I upgraded from 0.5.x to 0.6.x? How do I get them back? ###

Yeah, sorry about that. The 0.5.x versions stored your saved streams as part of your settings, but Windows keeps a separate settings file for each installed version, file location, etc. So it was not the best place for the library. As of version 0.6.0 the streams library is kept in a version-independent location.

Your streams library is not lost, however -- it's just kept in a location Auremo is no longer looking for. You can re-install version 0.5.1 (maybe just grab the no-installer version for this) and you can see your streams again. Then select all of them and right-click to export them to a file. Now you can restart Auremo 0.6.x and load the streams from the file.
