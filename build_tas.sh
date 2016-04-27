#!/bin/bash
cd FEZ.TAS.mm
xbuild
xbuild /p:Configuration='Debug - FNA'
cd ..
