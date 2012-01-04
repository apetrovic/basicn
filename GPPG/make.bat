@echo off
gppg /gplex /out:..\Parser\BNParser.y.cs /no-lines BNParser.y
gplex /out:..\Parser\BNParserLex.cs BNParserLex.lex