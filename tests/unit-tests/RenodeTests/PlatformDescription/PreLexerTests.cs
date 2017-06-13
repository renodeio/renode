﻿﻿//
// Copyright (c) Antmicro
//
// This file is part of the Renode project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using System.Linq;
using Emul8.PlatformDescription;
using NUnit.Framework;

namespace Antmicro.Renode.UnitTests.PlatformDescription
{
    [TestFixture]
    public class PreLexerTests
    {
        [Test]
        public void ShouldProcessEmptyFile()
        {
            var result = PreLexer.Process(new string[0]);
            CollectionAssert.AreEquivalent(new string[0], result);
        }

        [Test]
        public void ShouldProcessSimpleFile()
        {
            var source = @"first line
second line
    first indented
    second indented
third line";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"first line;
second line{
    first indented;
    second indented};
third line");

            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void ShouldProcessDoubleDedent()
        {
            var source = @"first line
second line
    first indented
    second indented

third line";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"first line;
second line{
    first indented;
    second indented};

third line");

            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void ShouldProcessDoubleDedentAtTheEndOfFile()
        {
            var source = @"first line
second line
    first indented";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"first line;
second line{
    first indented}");

            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void ShouldProcessTwoLinesWithNoIndent()
        {
            var source = @"
line1
line2";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1;
line2");
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void ShouldProcessTwoLinesWithNoIndentAndSeparation()
        {
            var source = @"
line1

line2";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1;

line2");
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void ShouldHandleEmptyLinesAtTheEndOfSource()
        {
            var source = @"
line1
line2

";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1;
line2

");
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void ShouldNotProcessIndentInBraces()
        {
var source = @"
line1 { 
    line2 }";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1 { 
    line2 }");
            
            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldHandleLineComments()
        {
            var source = @"
line1 {// something
    line2 }";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1 {
    line2 }");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldHandleLineCommentsAndStrings()
        {
            var source = @"
line1 ""something with //"" ""another // pseudo comment"" { // and here goes real comment
    line2 }";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1 ""something with //"" ""another // pseudo comment"" { 
    line2 }");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldFailOnUnterminatedString()
        {
            var source = @"
line1 ""i'm unterminated";

            var result = PreLexer.Process(SplitUsingNewline(source));
            var exception = Assert.Throws<ParsingException>(() => result.ToArray());
            Assert.AreEqual(ParsingError.SyntaxError, exception.Error);
        }

        [Test]
        public void ShouldHandleMultilineCommentsInOneLine()
        {
            var source = @"
line1 used as a ruler1234            123456789                1234
line 2/* first comment*/ ""string with /*"" /* second comment */ something";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1 used as a ruler1234            123456789                1234;
line 2                   ""string with /*""                      something");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldHandleMultilineComments()
        {
            var source = @"
line1/* here we begin
    here it goes
more
more
    here we finish*/
line2";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1;




line2");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldHandleMultilineCommentsWithinBraces()
        {
            var source = @"
line1 { /* here we begin
    here it goes
here the comment ends*/ x: 5 }
line2";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1 { 

                        x: 5 };
line2");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldFailIfTheMultilineCommentFinishesBeforeEndOfLine()
        {
            var source = @"
line1 /* here we begin
    here it goes
here the comment ends*/ x: 5
line2";

            var result = PreLexer.Process(SplitUsingNewline(source));
            var exception = Assert.Throws<ParsingException>(() => result.ToArray());
            Assert.AreEqual(ParsingError.SyntaxError, exception.Error);
        }

        [Test]
        public void ShouldProcessBraceInString()
        {
            var source = @"
line1 ""{ \"" {""
line2";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var expectedResult = SplitUsingNewline(@"
line1 ""{ \"" {"";
line2");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldHandleTextOnTheFirstLine()
        {
            var source = @"onlyLine";

            var result = PreLexer.Process(SplitUsingNewline(source)).ToArray();

            var expectedResult = SplitUsingNewline(@"onlyLine");

            CollectionAssert.AreEqual(expectedResult, result);
        }

        [Test]
        public void ShouldHandleInitialIndent()
        {
            var source = @"
    first line
    second line
        first indented
        second indented
    third line";
            var result = PreLexer.Process(SplitUsingNewline(source));

            var exception = Assert.Throws<ParsingException>(() => result.ToArray());
            Assert.AreEqual(ParsingError.WrongIndent, exception.Error);
        }

        [Test]
        public void ShouldFailOnSingleLineMultilineComment()
        {
            var source = @"
first line
    /*something*/ second line";

            var result = PreLexer.Process(SplitUsingNewline(source));

            var exception = Assert.Throws<ParsingException>(() => result.ToArray());
            Assert.AreEqual(ParsingError.SyntaxError, exception.Error);
        }

        private static string[] SplitUsingNewline(string source)
        {
            return source.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        }
    }
}
