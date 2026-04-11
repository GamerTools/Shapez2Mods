#!/usr/bin/env bash

#CONTENT_PATH=$1
CONTENT_PATH="/mnt/c/Users/garre/AppData/LocalLow/tobspr Games/shapez 2/mods/CornerCutter"

# Composes the location of the preview image
PREVIEW_IMG="${PWD}/Steam/preview.png"

echo "CONTENT_PATH: $CONTENT_PATH"
echo "PREVIEW_IMG: $PREVIEW_IMG"

export CONTENT_PATH
export PREVIEW_IMG

# Adjust temporary .vdf with absolute paths for the content and the preview image
TMP_VDF=Steam/mod.vdf
envsubst < Steam/temp.vdf > $TMP_VDF

# Log the final version
cat $TMP_VDF

# Execute
steamcmd +login fatcatx +workshop_build_item "$TMP_VDF" +quit;

# Copy published file id back
cat $TMP_VDF

# Grab published file id 
FILE_ID=$(grep '"publishedfileid"' Steam/base.tmp.vdf | sed 's/.*"publishedfileid"[ \t]*"\([0-9]\+\)".*/\1/')

# Updating original file with new published file ID
echo "New published file ID: $FILE_ID"
sed -i 's/\("publishedfileid"[ \t]*"\)[0-9]\+"/\1'"$FILE_ID"'"/' Steam/base.vdf

# Clean temporary files
rm $TMP_VDF