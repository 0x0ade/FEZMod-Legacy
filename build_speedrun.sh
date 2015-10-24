#!/bin/bash
cd FEZ.Speedrun.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
