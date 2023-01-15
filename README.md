# lz4cmd
High-performance ```lz4``` command line tool by two-way async io operation.

# Syntax
## Compression
```
lz4cmd INFILE OUTFILE
```

## Deompression
```
lz4cmd -d INFILE OUTFILE
```

## Short form for standard input and standard output
* If ```INFILE``` is skipped or "-" is referred, the program would use redirected standard input.
* If ```OUTFILE``` is skipped or "-" is referred, the program would use standard output.

## Credits
* LZ4 is created by Yann Collet (https://github.com/lz4/lz4).
* LZ4.NET is created by Ewout van der Linden (https://github.com/IonKiwi/lz4.managed) and is contributed by Bar Arnon.

## Copyright
v1.0.0.3

Yung, Chun Kau (yung.chun.kau@gmail.com) 2023 Jan

yung.chun.kau@gmail.com
