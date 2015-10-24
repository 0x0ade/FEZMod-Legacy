#!/bin/bash
cd MonoGame.Framework.FML.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
