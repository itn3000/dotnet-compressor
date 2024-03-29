# Single binary compression tool

This is the tool for compressing, archiving.

# Features

* supporting following formats
    * container
        * [ZIP(encryption is OK)](https://tools.ietf.org/html/rfc1950)
        * [TAR](https://www.freebsd.org/cgi/man.cgi?query=tar&sektion=5&manpath=FreeBSD+8-current)
    * compression
        * [GZIP](https://tools.ietf.org/html/rfc1952)
        * [BZIP2](http://www.bzip.org/)
        * [LZIP](https://www.nongnu.org/lzip/)
        * [XZ(decompression only)](https://tukaani.org/xz/)
* you can use as single binary executable(powered by [corert](https://github.com/dotnet/corert))
    * linux-x64,macos-x64 and windows-x64 are supported
* you can use as [dotnet global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)

# Known limitation

* XZ is decompression only
* File attributes in archive is not preserved
* Format is not detected by automatic
* Symblic link not supported

# Usage

## Getting Binary

### Single Binary

you can get binary from [github release page](https://github.com/itn3000/dotnet-compressor/releases).
Once you download it and add executable permission, you can execute it.

### dotnet global tool

if you want to use as dotnet global tool, do following steps.

1. install [dotnet sdk](https://dotnet.microsoft.com/download) 5.0 or later
2. run `dotnet tool install -g dotnet-compressor`
3. ensure `$HOME/.dotnet/tools` to your PATH

## Basic

the command's basic format is `dcomp [kind] [verb(d or c)] [options]`.
you can get kind list by `dcomp --help`,
verb is `c(compression)` or `d(decompression)`.
and you can get individual subcommand help is `dcomp [kind] [verb] --help`

## ZIP

you can manupilate ZIP by `zip` subcommand.

### Basic Usage

* archiving files under `dir1` and output `dir1.zip`: `dcomp zip c -b dir1 -o dir1.zip`.
* extracting `dir1.zip` into `dir1`: `dcomp zip d -i dir1.zip -o dir1`

### File globbing

* archiving files under `dir1` which has `.txt` extension: `dcomp zip c -b dir1 -i '**/*.txt' -o dir1.zip`
* extracting files from `dir1.zip` which has `.txt` extension to `dir1`: `dcomp zip d -i dir1.zip --include '**/*.txt' -o dir1`

### Converting filename encoding 

* archiving files and convert filename to Shift-JIS: `dcomp zip c -e sjis -b dir1 -o dir1.zip`
* extracting `dir1.zip` and filename decoding as Shift-JIS: `dcomp zip d -e sjis -i dir1.zip -o dir1`

### Password encryption

* archiving files and encrypting with password `abc`: `dcomp zip c -b dir1 -o dir1.zip -p abc`
* archiving files and decrypting with password `abc`: `dcomp zip d -i dir1.zip -o dir1 -p abc`

## TAR

### Basic Usage

* archiving files under `dir1` and output `dir1.tar`: `dcomp tar c -b dir1 -o dir1.tar`.
* extracting `dir1.tar` into `dir1`: `dcomp tar d -i dir1.tar -o dir1`

### STDIO

* archiving files under `dir1` and output to standard output: `dcomp tar c -b dir1 > dir1.tar`
* extracting data from standard input into `dir1`: `cat dir1.tar | dcomp tar d -o dir1`

### File globbing

* archiving files under `dir1` which has `.txt` extension: `dcomp tar c -b dir1 -i '**/*.txt' -o dir1.tar`
* extracting files from `dir1.tar` which has `.txt` extension to `dir1`: `dcomp tar d -i '**/*.txt' -o dir1`

### Converting filename encoding 

* archiving files and convert filename to Shift-JIS: `dcomp tar c -e sjis -b dir1 -o dir1.tar`
* extracting `dir1.tar` and filename decoding as Shift-JIS: `dcomp tar d -e sjis -i dir1.tar -o dir1`

### set file permission(since 1.1.0)

* set file permission '0755' for files which have '*.sh' extension: `dcomp tar c -b dir1 -o dir1.tar -pm ".*\.sh=755"`

## GZIP

### Basic Usage

* compress `test.txt` and output to `test.gz`: `dcomp gz c -i test.txt -o test.gz`
* decompress `test.gz` and output to `test.txt`: `dcomp gz d -i test.gz -o test.txt`

## BZ2

### Basic Usage

* compress `test.txt` and output to `test.bz2`: `dcomp bz2 c -i test.txt -o test.bz2`
* decompress `test.bz2` and output to `test.txt`: `dcomp bz2 d -i test.bz2 -o test.txt`

## LZIP

### Basic Usage

* compress `test.txt` and output to `test.lz`: `dcomp lzip c -i test.txt -o test.lz`
* decompress `test.lz` and output to `test.txt`: `dcomp lzip d -i test.bz2 -o test.txt`

## XZ

### Basic Usage

* extracting `test.xz` and output to `test.txt`: `dcomp xz d -i test.xz -o test.txt`

## GZIP+TAR

* archiving files under `dir1` and compressing by gzip: `dcomp tar c -b dir1 | dcomp gz c -o dir1.tgz`
* extracting from `dir1.tgz` into `dir1`: `dcomp gz d -i dir1.tgz|dcomp tar d -o dir1`

## ZStandard

### Basic Usage

* compressing `test.txt` and output to `test.zstd`: `dcomp zstd c -i test.txt -o test.zstd`
* decompressing `test.zstd` and output to `test.txt`: `dcomp zstd d -i test.zstd -o test.txt`

# How to build

## Prerequisits

* dotnet-sdk 5.0
* mono(if linux or mac)
* .NET Framework 4.6.1 or later(if windows)

here are prerequisits if you want to build native binary

* windows
    * Build tools for Visual Studio 2019(if windows)
* ubuntu
    * build-essential
    * libkrb5-dev
    * zlib1g-dev
    * clang
* mac
    * xcode

## Build 

1. run `dotnet tool restore`
2. run `dotnet tool run dotnet-cake`
    * if you want to build release binary, run `dotnet tool run dotnet-cake -Configuration=Release -IsRelease`

and then you will get nupkg in `dist/[Configuration]/nupkg`, binary executable in `bin/Release/netcoreapp3.1`

## Build native binary

if you want to get single executable binary,
you should do following steps.

1. set `CppCompilerAndLinker=clang-[version]` env value if you do not have clang-3.9 and build in linux or macos.
2. run `dotnet tool run dotnet-cake -Target=Native -Runtime=[rid] -Configuration=[Debug or Release]`

[about rid, refer to this page](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)

and then you will get native executable binary in `dist/[Configuration]/bin`

## Generating sln

sln is not existed in repo.if you want to solution file(*.sln), you should do following steps.

1. run `dotnet tool run dotnet-cake -Target=SlnGen`

and then you will get `dotnet-compressor.sln` file in top of the repo.
