#!/bin/bash
cd EasyStorage.Mod.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
