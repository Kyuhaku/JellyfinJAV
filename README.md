# JellyfinJav
Don't expect perfection.

# Metadata Providers
* R18.dev (Videos)
* JAVLibrary (Videos) - Broken
* JavTrailers (Videos)
* AsianScreens (Actresses)
* Warashi Asian Pornstars (Actresses)

# Instructions
### Installation

Currently you'll have to download the release zip then extract it into your plugins folder, looking into fixing the manifest and repo so you can install it via the catalog within jellyfin.


### Usage
When adding the media library, make sure to select "Content type: movies".

### Example File Names
* abp200.mkv
* ABP200.mkv
* ABP-200.mkv
* some random text abp-200 more random text.mkv
> This should still be how it works, I didn't change any of the filename detection, I've only ever tested with ABP-200 type of file naming.

# Development
### Requirements
* Docker
* Docker Compose
* Python
* .NET 5.0

### Building
    $ ./build.sh
    # Visit localhost:8096

### Packaging
    $ python package.py
    # manifest.json will update, and the package will be zipped up in release/

### General
JAV files for testing purposes are stored in videos/

# Screenshots
![Grid Example](screenshots/example-grid.jpg)
![Video Example](screenshots/example-video.jpg)
![Actress Example](screenshots/example-actress.jpg)

# License
Licensed under AGPL-3.0-only
