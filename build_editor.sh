#!/bin/bash
cd FEZ.Editor.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
