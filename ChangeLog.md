* 2.0.1
    * fix default compression level(zip and gzip)
* 2.0.0
    * **update TargetFramework to net6.0**
    * fix wrong typeflag of tar
* 1.2.0
    * **update TargetFramework to net5.0**
    * add `--retry` to `tar c` and `zip c` command
    * add `--stop-on-error` to `tar c` and `zip c` command
    * add `--verbose` to `tar c` and `zip c` and `tar d` and `zip d` command
* 1.1.0
    * add `--permission-map` and `--permission-file` option to `tar c` command(default 0644).
* 1.0.1
    * fix filetime problem in tar archiving
* 1.0.0
    * support constructing/extracting tar+gz,tar+bzip2,tar+lzip in one command
    * change tfm to netcoreapp3.1
    * use SharpZipLib instead of sharpcompress for tar format
* 0.1.0
    * First release
