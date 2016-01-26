@echo off
REM mkdir FEZMOD
REM copy ${BUILD_DIR}/*.mm/bin/Debug/*.mm.dll ./FEZMOD/
REM copy ${BUILD_DIR}/*.mm/bin/Debug/*.mm.dll.mdb ./FEZMOD/
REM mkdir FEZMOD-FNA
REM copy ${BUILD_DIR}/*.mm/bin/DebugFNA/*.mm.dll ./FEZMOD-FNA/
REM copy ${BUILD_DIR}/*.mm/bin/DebugFNA/*.mm.dll.mdb ./FEZMOD-FNA/

echo Creating directories
rmdir /s/q FEZModInstallerGen
mkdir FEZModInstallerGen
mkdir FEZModInstallerGen\FEZMOD
mkdir FEZModInstallerGen\FEZMOD-FNA

for /d %%d in (*.mm) do (
	echo Processing directory %%d
	for %%f in (%%d\bin\Debug\*.mm.dll) do (
		echo Copying %%~nf.dll
		copy %%~ff FEZModInstallerGen\FEZMOD\%%~nf.dll
	)
	for %%f in (%%d\bin\DebugFNA\*.mm.dll) do (
		echo Copying %%~nf.dll [FNA]
		copy %%~ff FEZModInstallerGen\FEZMOD-FNA\%%~nf.dll
	)
)

pause
