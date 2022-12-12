# lz4cmd
high-performance lz4 tool (command line)

# Syntax
## Compression
<code>lz4 INFILE OUTFILE</code>

## Deompression
<code>lz4 -d INFILE OUTFILE</code>

## Short form for standard input and standard output
* If INFILE is skipped or "-" is referred, the program would use redirected standard input.
* If OUTFILE is skipped or "-" is referred, the program would use standard output.

## Credits
* LZ4 is created by Yann Collet (https://github.com/lz4/lz4).
* LZ4.NET is created by Ewout van der Linden (https://github.com/IonKiwi/lz4.managed) and is contributed by Bar Arnon.

## Copyright
* Yung, Chun Kau (yung.chun.kau@gmail.com) 2022 December
