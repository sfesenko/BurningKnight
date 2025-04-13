#!/bin/bash

# This script build and prepare Content
# Before start, need to setup Wine For Effect Compilation:
# https://docs.monogame.net/articles/getting_started/1_setting_up_your_os_for_development_ubuntu.html?tabs=android#setup-wine-for-effect-compilation 

MGFXC_WINE_PATH=~/.winemonogame/  ~/.dotnet/tools/mgcb Content/Content.mgcb
mv Content/bin/Content/* Content/bin
rm -rf Content/bin/Content
