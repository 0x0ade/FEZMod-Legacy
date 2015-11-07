#!/bin/bash
cd Common.Mod.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
