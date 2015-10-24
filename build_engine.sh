#!/bin/bash
cd FezEngine.Mod.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
