﻿// <auto-generated/>
#nullable enable
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // Unreferenced label
#pragma warning disable CS0219 // Variable assigned but never used

namespace ModManager.ViewModels.Mods
{
    partial class ModListViewModel
    {
        /// <remarks>
        /// Pattern:<br/>
        /// <code>@([^\\s]+?)([\\s]+)([^@\\s]*)</code><br/>
        /// Explanation:<br/>
        /// <code>
        /// ○ Match '@'.<br/>
        /// ○ 1st capture group.<br/>
        ///     ○ Match a character in the set [^\s] atomically at least once.<br/>
        /// ○ 2nd capture group.<br/>
        ///     ○ Match a whitespace character greedily at least once.<br/>
        /// ○ 3rd capture group.<br/>
        ///     ○ Match a character in the set [^@\s] atomically any number of times.<br/>
        /// </code>
        /// </remarks>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
        private static partial global::System.Text.RegularExpressions.Regex FilterPropertyRe() => global::System.Text.RegularExpressions.Generated.FilterPropertyRe_0.Instance;
    }
}

namespace ModManager.ViewModels.Mods
{
    partial class ModListViewModel
    {
        /// <remarks>
        /// Pattern:<br/>
        /// <code>@([^\\s]+?)([\\s"]+)([^@"]*)</code><br/>
        /// Explanation:<br/>
        /// <code>
        /// ○ Match '@'.<br/>
        /// ○ 1st capture group.<br/>
        ///     ○ Match a character in the set [^\s] lazily at least once.<br/>
        /// ○ 2nd capture group.<br/>
        ///     ○ Match a character in the set ["\s] greedily at least once.<br/>
        /// ○ 3rd capture group.<br/>
        ///     ○ Match a character in the set [^"@] atomically any number of times.<br/>
        /// </code>
        /// </remarks>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
        private static partial global::System.Text.RegularExpressions.Regex FilterPropertyPatternWithQuotesRe() => global::System.Text.RegularExpressions.Generated.FilterPropertyPatternWithQuotesRe_1.Instance;
    }
}

namespace ModManager.ViewModels
{
    partial class AppUpdateWindowViewModel
    {
        /// <remarks>
        /// Pattern:<br/>
        /// <code>^\\s+$[\\r\\n]*</code><br/>
        /// Options:<br/>
        /// <code>RegexOptions.Multiline</code><br/>
        /// Explanation:<br/>
        /// <code>
        /// ○ Match if at the beginning of a line.<br/>
        /// ○ Match a whitespace character greedily at least once.<br/>
        /// ○ Match if at the end of a line.<br/>
        /// ○ Match a character in the set [\n\r] atomically any number of times.<br/>
        /// </code>
        /// </remarks>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
        private static partial global::System.Text.RegularExpressions.Regex RemoveEmptyLinesRe() => global::System.Text.RegularExpressions.Generated.RemoveEmptyLinesRe_2.Instance;
    }
}

namespace System.Text.RegularExpressions.Generated
{
    using System;
    using System.Buffers;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>Custom <see cref="Regex"/>-derived type for the FilterPropertyRe method.</summary>
    [GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
    file sealed class FilterPropertyRe_0 : Regex
    {
        /// <summary>Cached, thread-safe singleton instance.</summary>
        internal static readonly FilterPropertyRe_0 Instance = new();
    
        /// <summary>Initializes the instance.</summary>
        private FilterPropertyRe_0()
        {
            base.pattern = "@([^\\s]+?)([\\s]+)([^@\\s]*)";
            base.roptions = RegexOptions.None;
            ValidateMatchTimeout(Utilities.s_defaultTimeout);
            base.internalMatchTimeout = Utilities.s_defaultTimeout;
            base.factory = new RunnerFactory();
            base.capsize = 4;
        }
    
        /// <summary>Provides a factory for creating <see cref="RegexRunner"/> instances to be used by methods on <see cref="Regex"/>.</summary>
        private sealed class RunnerFactory : RegexRunnerFactory
        {
            /// <summary>Creates an instance of a <see cref="RegexRunner"/> used by methods on <see cref="Regex"/>.</summary>
            protected override RegexRunner CreateInstance() => new Runner();
        
            /// <summary>Provides the runner that contains the custom logic implementing the specified regular expression.</summary>
            private sealed class Runner : RegexRunner
            {
                /// <summary>Scan the <paramref name="inputSpan"/> starting from base.runtextstart for the next match.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                protected override void Scan(ReadOnlySpan<char> inputSpan)
                {
                    // Search until we can't find a valid starting position, we find a match, or we reach the end of the input.
                    while (TryFindNextPossibleStartingPosition(inputSpan) &&
                           !TryMatchAtCurrentPosition(inputSpan) &&
                           base.runtextpos != inputSpan.Length)
                    {
                        base.runtextpos++;
                        if (Utilities.s_hasTimeout)
                        {
                            base.CheckTimeout();
                        }
                    }
                }
        
                /// <summary>Search <paramref name="inputSpan"/> starting from base.runtextpos for the next location a match could possibly start.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                /// <returns>true if a possible match was found; false if no more matches are possible.</returns>
                private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
                {
                    int pos = base.runtextpos;
                    char ch;
                    
                    // Any possible match is at least 3 characters.
                    if (pos <= inputSpan.Length - 3)
                    {
                        // The pattern begins with a character in the set @.
                        // Find the next occurrence. If it can't be found, there's no match.
                        ReadOnlySpan<char> span = inputSpan.Slice(pos);
                        for (int i = 0; i < span.Length - 2; i++)
                        {
                            int indexOfPos = span.Slice(i).IndexOf('@');
                            if (indexOfPos < 0)
                            {
                                goto NoMatchFound;
                            }
                            i += indexOfPos;
                            
                            // The primary set being searched for was found. 1 more set will be checked so as
                            // to minimize the number of places TryMatchAtCurrentPosition is run unnecessarily.
                            // Make sure it fits in the remainder of the input.
                            if ((uint)(i + 1) >= (uint)span.Length)
                            {
                                goto NoMatchFound;
                            }
                            
                            if (((ch = span[i + 1]) < 128 ? ("쇿\uffff\ufffe\uffff\uffff\uffff\uffff\uffff"[ch >> 4] & (1 << (ch & 0xF))) != 0 : RegexRunner.CharInClass((char)ch, "\u0001\0\u0001d")))
                            {
                                base.runtextpos = pos + i;
                                return true;
                            }
                        }
                    }
                    
                    // No match found.
                    NoMatchFound:
                    base.runtextpos = inputSpan.Length;
                    return false;
                }
        
                /// <summary>Determine whether <paramref name="inputSpan"/> at base.runtextpos is a match for the regular expression.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                /// <returns>true if the regular expression matches at the current position; otherwise, false.</returns>
                private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
                {
                    int pos = base.runtextpos;
                    int matchStart = pos;
                    char ch;
                    int capture_starting_pos = 0;
                    int capture_starting_pos1 = 0;
                    int capture_starting_pos2 = 0;
                    int charloop_capture_pos = 0;
                    int charloop_starting_pos = 0, charloop_ending_pos = 0;
                    ReadOnlySpan<char> slice = inputSpan.Slice(pos);
                    
                    // Match '@'.
                    if (slice.IsEmpty || slice[0] != '@')
                    {
                        UncaptureUntil(0);
                        return false; // The input didn't match.
                    }
                    
                    // 1st capture group.
                    {
                        pos++;
                        slice = inputSpan.Slice(pos);
                        capture_starting_pos = pos;
                        
                        // Match a character in the set [^\s] atomically at least once.
                        {
                            int iteration = 0;
                            while ((uint)iteration < (uint)slice.Length && ((ch = slice[iteration]) < 128 ? ("쇿\uffff\ufffe\uffff\uffff\uffff\uffff\uffff"[ch >> 4] & (1 << (ch & 0xF))) != 0 : RegexRunner.CharInClass((char)ch, "\u0001\0\u0001d")))
                            {
                                iteration++;
                            }
                            
                            if (iteration == 0)
                            {
                                UncaptureUntil(0);
                                return false; // The input didn't match.
                            }
                            
                            slice = slice.Slice(iteration);
                            pos += iteration;
                        }
                        
                        base.Capture(1, capture_starting_pos, pos);
                    }
                    
                    // 2nd capture group.
                    //{
                        capture_starting_pos1 = pos;
                        
                        // Match a whitespace character greedily at least once.
                        //{
                            charloop_starting_pos = pos;
                            
                            int iteration1 = 0;
                            while ((uint)iteration1 < (uint)slice.Length && char.IsWhiteSpace(slice[iteration1]))
                            {
                                iteration1++;
                            }
                            
                            if (iteration1 == 0)
                            {
                                UncaptureUntil(0);
                                return false; // The input didn't match.
                            }
                            
                            slice = slice.Slice(iteration1);
                            pos += iteration1;
                            
                            charloop_ending_pos = pos;
                            charloop_starting_pos++;
                            goto CharLoopEnd;
                            
                            CharLoopBacktrack:
                            UncaptureUntil(charloop_capture_pos);
                            
                            if (Utilities.s_hasTimeout)
                            {
                                base.CheckTimeout();
                            }
                            
                            if (charloop_starting_pos >= charloop_ending_pos)
                            {
                                UncaptureUntil(0);
                                return false; // The input didn't match.
                            }
                            pos = --charloop_ending_pos;
                            slice = inputSpan.Slice(pos);
                            
                            CharLoopEnd:
                            charloop_capture_pos = base.Crawlpos();
                        //}
                        
                        base.Capture(2, capture_starting_pos1, pos);
                        
                        goto CaptureSkipBacktrack;
                        
                        CaptureBacktrack:
                        goto CharLoopBacktrack;
                        
                        CaptureSkipBacktrack:;
                    //}
                    
                    // 3rd capture group.
                    {
                        capture_starting_pos2 = pos;
                        
                        // Match a character in the set [^@\s] atomically any number of times.
                        {
                            int iteration2 = 0;
                            while ((uint)iteration2 < (uint)slice.Length && ((ch = slice[iteration2]) < 128 ? ("쇿\uffff\ufffe\uffff\ufffe\uffff\uffff\uffff"[ch >> 4] & (1 << (ch & 0xF))) != 0 : RegexRunner.CharInClass((char)ch, "\u0001\u0002\u0001@Ad")))
                            {
                                iteration2++;
                            }
                            
                            slice = slice.Slice(iteration2);
                            pos += iteration2;
                        }
                        
                        base.Capture(3, capture_starting_pos2, pos);
                    }
                    
                    // The input matched.
                    base.runtextpos = pos;
                    base.Capture(0, matchStart, pos);
                    return true;
                    
                    // <summary>Undo captures until it reaches the specified capture position.</summary>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void UncaptureUntil(int capturePosition)
                    {
                        while (base.Crawlpos() > capturePosition)
                        {
                            base.Uncapture();
                        }
                    }
                }
            }
        }

    }
    
    /// <summary>Custom <see cref="Regex"/>-derived type for the FilterPropertyPatternWithQuotesRe method.</summary>
    [GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
    file sealed class FilterPropertyPatternWithQuotesRe_1 : Regex
    {
        /// <summary>Cached, thread-safe singleton instance.</summary>
        internal static readonly FilterPropertyPatternWithQuotesRe_1 Instance = new();
    
        /// <summary>Initializes the instance.</summary>
        private FilterPropertyPatternWithQuotesRe_1()
        {
            base.pattern = "@([^\\s]+?)([\\s\"]+)([^@\"]*)";
            base.roptions = RegexOptions.None;
            ValidateMatchTimeout(Utilities.s_defaultTimeout);
            base.internalMatchTimeout = Utilities.s_defaultTimeout;
            base.factory = new RunnerFactory();
            base.capsize = 4;
        }
    
        /// <summary>Provides a factory for creating <see cref="RegexRunner"/> instances to be used by methods on <see cref="Regex"/>.</summary>
        private sealed class RunnerFactory : RegexRunnerFactory
        {
            /// <summary>Creates an instance of a <see cref="RegexRunner"/> used by methods on <see cref="Regex"/>.</summary>
            protected override RegexRunner CreateInstance() => new Runner();
        
            /// <summary>Provides the runner that contains the custom logic implementing the specified regular expression.</summary>
            private sealed class Runner : RegexRunner
            {
                /// <summary>Scan the <paramref name="inputSpan"/> starting from base.runtextstart for the next match.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                protected override void Scan(ReadOnlySpan<char> inputSpan)
                {
                    // Search until we can't find a valid starting position, we find a match, or we reach the end of the input.
                    while (TryFindNextPossibleStartingPosition(inputSpan) &&
                           !TryMatchAtCurrentPosition(inputSpan) &&
                           base.runtextpos != inputSpan.Length)
                    {
                        base.runtextpos++;
                        if (Utilities.s_hasTimeout)
                        {
                            base.CheckTimeout();
                        }
                    }
                }
        
                /// <summary>Search <paramref name="inputSpan"/> starting from base.runtextpos for the next location a match could possibly start.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                /// <returns>true if a possible match was found; false if no more matches are possible.</returns>
                private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
                {
                    int pos = base.runtextpos;
                    char ch;
                    
                    // Any possible match is at least 3 characters.
                    if (pos <= inputSpan.Length - 3)
                    {
                        // The pattern begins with a character in the set @.
                        // Find the next occurrence. If it can't be found, there's no match.
                        ReadOnlySpan<char> span = inputSpan.Slice(pos);
                        for (int i = 0; i < span.Length - 2; i++)
                        {
                            int indexOfPos = span.Slice(i).IndexOf('@');
                            if (indexOfPos < 0)
                            {
                                goto NoMatchFound;
                            }
                            i += indexOfPos;
                            
                            // The primary set being searched for was found. 1 more set will be checked so as
                            // to minimize the number of places TryMatchAtCurrentPosition is run unnecessarily.
                            // Make sure it fits in the remainder of the input.
                            if ((uint)(i + 1) >= (uint)span.Length)
                            {
                                goto NoMatchFound;
                            }
                            
                            if (((ch = span[i + 1]) < 128 ? ("쇿\uffff\ufffe\uffff\uffff\uffff\uffff\uffff"[ch >> 4] & (1 << (ch & 0xF))) != 0 : RegexRunner.CharInClass((char)ch, "\u0001\0\u0001d")))
                            {
                                base.runtextpos = pos + i;
                                return true;
                            }
                        }
                    }
                    
                    // No match found.
                    NoMatchFound:
                    base.runtextpos = inputSpan.Length;
                    return false;
                }
        
                /// <summary>Determine whether <paramref name="inputSpan"/> at base.runtextpos is a match for the regular expression.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                /// <returns>true if the regular expression matches at the current position; otherwise, false.</returns>
                private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
                {
                    int pos = base.runtextpos;
                    int matchStart = pos;
                    char ch;
                    int capture_starting_pos = 0;
                    int capture_starting_pos1 = 0;
                    int capture_starting_pos2 = 0;
                    int charloop_capture_pos = 0;
                    int charloop_starting_pos = 0, charloop_ending_pos = 0;
                    int lazyloop_capturepos = 0;
                    int lazyloop_pos = 0;
                    ReadOnlySpan<char> slice = inputSpan.Slice(pos);
                    
                    // Match '@'.
                    if (slice.IsEmpty || slice[0] != '@')
                    {
                        UncaptureUntil(0);
                        return false; // The input didn't match.
                    }
                    
                    // 1st capture group.
                    //{
                        pos++;
                        slice = inputSpan.Slice(pos);
                        capture_starting_pos = pos;
                        
                        // Match a character in the set [^\s] lazily at least once.
                        //{
                            if (slice.IsEmpty || ((ch = slice[0]) < 128 ? ("쇿\uffff\ufffe\uffff\uffff\uffff\uffff\uffff"[ch >> 4] & (1 << (ch & 0xF))) == 0 : !RegexRunner.CharInClass((char)ch, "\u0001\0\u0001d")))
                            {
                                UncaptureUntil(0);
                                return false; // The input didn't match.
                            }
                            
                            pos++;
                            slice = inputSpan.Slice(pos);
                            lazyloop_pos = pos;
                            goto LazyLoopEnd;
                            
                            LazyLoopBacktrack:
                            UncaptureUntil(lazyloop_capturepos);
                            if (Utilities.s_hasTimeout)
                            {
                                base.CheckTimeout();
                            }
                            
                            pos = lazyloop_pos;
                            slice = inputSpan.Slice(pos);
                            if (slice.IsEmpty || ((ch = slice[0]) < 128 ? ("쇿\uffff\ufffe\uffff\uffff\uffff\uffff\uffff"[ch >> 4] & (1 << (ch & 0xF))) == 0 : !RegexRunner.CharInClass((char)ch, "\u0001\0\u0001d")))
                            {
                                UncaptureUntil(0);
                                return false; // The input didn't match.
                            }
                            pos++;
                            slice = inputSpan.Slice(pos);
                            lazyloop_pos = pos;
                            
                            LazyLoopEnd:
                            lazyloop_capturepos = base.Crawlpos();
                        //}
                        
                        base.Capture(1, capture_starting_pos, pos);
                        
                        goto CaptureSkipBacktrack;
                        
                        CaptureBacktrack:
                        goto LazyLoopBacktrack;
                        
                        CaptureSkipBacktrack:;
                    //}
                    
                    // 2nd capture group.
                    //{
                        capture_starting_pos1 = pos;
                        
                        // Match a character in the set ["\s] greedily at least once.
                        //{
                            charloop_starting_pos = pos;
                            
                            int iteration = 0;
                            while ((uint)iteration < (uint)slice.Length && ((ch = slice[iteration]) < 128 ? ("㸀\0\u0005\0\0\0\0\0"[ch >> 4] & (1 << (ch & 0xF))) != 0 : RegexRunner.CharInClass((char)ch, "\0\u0002\u0001\"#d")))
                            {
                                iteration++;
                            }
                            
                            if (iteration == 0)
                            {
                                goto CaptureBacktrack;
                            }
                            
                            slice = slice.Slice(iteration);
                            pos += iteration;
                            
                            charloop_ending_pos = pos;
                            charloop_starting_pos++;
                            goto CharLoopEnd;
                            
                            CharLoopBacktrack:
                            UncaptureUntil(charloop_capture_pos);
                            
                            if (Utilities.s_hasTimeout)
                            {
                                base.CheckTimeout();
                            }
                            
                            if (charloop_starting_pos >= charloop_ending_pos)
                            {
                                goto CaptureBacktrack;
                            }
                            pos = --charloop_ending_pos;
                            slice = inputSpan.Slice(pos);
                            
                            CharLoopEnd:
                            charloop_capture_pos = base.Crawlpos();
                        //}
                        
                        base.Capture(2, capture_starting_pos1, pos);
                        
                        goto CaptureSkipBacktrack1;
                        
                        CaptureBacktrack1:
                        goto CharLoopBacktrack;
                        
                        CaptureSkipBacktrack1:;
                    //}
                    
                    // 3rd capture group.
                    {
                        capture_starting_pos2 = pos;
                        
                        // Match a character in the set [^"@] atomically any number of times.
                        {
                            int iteration1 = slice.IndexOfAny('"', '@');
                            if (iteration1 < 0)
                            {
                                iteration1 = slice.Length;
                            }
                            
                            slice = slice.Slice(iteration1);
                            pos += iteration1;
                        }
                        
                        base.Capture(3, capture_starting_pos2, pos);
                    }
                    
                    // The input matched.
                    base.runtextpos = pos;
                    base.Capture(0, matchStart, pos);
                    return true;
                    
                    // <summary>Undo captures until it reaches the specified capture position.</summary>
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void UncaptureUntil(int capturePosition)
                    {
                        while (base.Crawlpos() > capturePosition)
                        {
                            base.Uncapture();
                        }
                    }
                }
            }
        }

    }
    
    /// <summary>Custom <see cref="Regex"/>-derived type for the RemoveEmptyLinesRe method.</summary>
    [GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
    file sealed class RemoveEmptyLinesRe_2 : Regex
    {
        /// <summary>Cached, thread-safe singleton instance.</summary>
        internal static readonly RemoveEmptyLinesRe_2 Instance = new();
    
        /// <summary>Initializes the instance.</summary>
        private RemoveEmptyLinesRe_2()
        {
            base.pattern = "^\\s+$[\\r\\n]*";
            base.roptions = RegexOptions.Multiline;
            ValidateMatchTimeout(Utilities.s_defaultTimeout);
            base.internalMatchTimeout = Utilities.s_defaultTimeout;
            base.factory = new RunnerFactory();
            base.capsize = 1;
        }
    
        /// <summary>Provides a factory for creating <see cref="RegexRunner"/> instances to be used by methods on <see cref="Regex"/>.</summary>
        private sealed class RunnerFactory : RegexRunnerFactory
        {
            /// <summary>Creates an instance of a <see cref="RegexRunner"/> used by methods on <see cref="Regex"/>.</summary>
            protected override RegexRunner CreateInstance() => new Runner();
        
            /// <summary>Provides the runner that contains the custom logic implementing the specified regular expression.</summary>
            private sealed class Runner : RegexRunner
            {
                /// <summary>Scan the <paramref name="inputSpan"/> starting from base.runtextstart for the next match.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                protected override void Scan(ReadOnlySpan<char> inputSpan)
                {
                    // Search until we can't find a valid starting position, we find a match, or we reach the end of the input.
                    while (TryFindNextPossibleStartingPosition(inputSpan) &&
                           !TryMatchAtCurrentPosition(inputSpan) &&
                           base.runtextpos != inputSpan.Length)
                    {
                        base.runtextpos++;
                        if (Utilities.s_hasTimeout)
                        {
                            base.CheckTimeout();
                        }
                    }
                }
        
                /// <summary>Search <paramref name="inputSpan"/> starting from base.runtextpos for the next location a match could possibly start.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                /// <returns>true if a possible match was found; false if no more matches are possible.</returns>
                private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
                {
                    int pos = base.runtextpos;
                    
                    // Empty matches aren't possible.
                    if ((uint)pos < (uint)inputSpan.Length)
                    {
                        // The pattern has a leading beginning-of-line anchor.
                        if (pos > 0 && inputSpan[pos - 1] != '\n')
                        {
                            int newlinePos = inputSpan.Slice(pos).IndexOf('\n');
                            if ((uint)newlinePos > inputSpan.Length - pos - 1)
                            {
                                goto NoMatchFound;
                            }
                            pos += newlinePos + 1;
                            
                            if (pos >= inputSpan.Length)
                            {
                                goto NoMatchFound;
                            }
                        }
                        
                        // The pattern begins with a whitespace character.
                        // Find the next occurrence. If it can't be found, there's no match.
                        int i = inputSpan.Slice(pos).IndexOfAnyWhiteSpace();
                        if (i >= 0)
                        {
                            base.runtextpos = pos + i;
                            return true;
                        }
                    }
                    
                    // No match found.
                    NoMatchFound:
                    base.runtextpos = inputSpan.Length;
                    return false;
                }
        
                /// <summary>Determine whether <paramref name="inputSpan"/> at base.runtextpos is a match for the regular expression.</summary>
                /// <param name="inputSpan">The text being scanned by the regular expression.</param>
                /// <returns>true if the regular expression matches at the current position; otherwise, false.</returns>
                private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
                {
                    int pos = base.runtextpos;
                    int matchStart = pos;
                    int charloop_starting_pos = 0, charloop_ending_pos = 0;
                    ReadOnlySpan<char> slice = inputSpan.Slice(pos);
                    
                    // Match if at the beginning of a line.
                    if (pos > 0 && inputSpan[pos - 1] != '\n')
                    {
                        return false; // The input didn't match.
                    }
                    
                    // Match a whitespace character greedily at least once.
                    //{
                        charloop_starting_pos = pos;
                        
                        int iteration = 0;
                        while ((uint)iteration < (uint)slice.Length && char.IsWhiteSpace(slice[iteration]))
                        {
                            iteration++;
                        }
                        
                        if (iteration == 0)
                        {
                            return false; // The input didn't match.
                        }
                        
                        slice = slice.Slice(iteration);
                        pos += iteration;
                        
                        charloop_ending_pos = pos;
                        charloop_starting_pos++;
                        goto CharLoopEnd;
                        
                        CharLoopBacktrack:
                        
                        if (Utilities.s_hasTimeout)
                        {
                            base.CheckTimeout();
                        }
                        
                        if (charloop_starting_pos >= charloop_ending_pos)
                        {
                            return false; // The input didn't match.
                        }
                        pos = --charloop_ending_pos;
                        slice = inputSpan.Slice(pos);
                        
                        CharLoopEnd:
                    //}
                    
                    // Match if at the end of a line.
                    if ((uint)pos < (uint)inputSpan.Length && inputSpan[pos] != '\n')
                    {
                        goto CharLoopBacktrack;
                    }
                    
                    // Match a character in the set [\n\r] atomically any number of times.
                    {
                        int iteration1 = slice.IndexOfAnyExcept('\n', '\r');
                        if (iteration1 < 0)
                        {
                            iteration1 = slice.Length;
                        }
                        
                        slice = slice.Slice(iteration1);
                        pos += iteration1;
                    }
                    
                    // The input matched.
                    base.runtextpos = pos;
                    base.Capture(0, matchStart, pos);
                    return true;
                }
            }
        }

    }
    
    /// <summary>Helper methods used by generated <see cref="Regex"/>-derived implementations.</summary>
    [GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "8.0.12.11118")]
    file static class Utilities
    {
        /// <summary>Default timeout value set in <see cref="AppContext"/>, or <see cref="Regex.InfiniteMatchTimeout"/> if none was set.</summary>
        internal static readonly TimeSpan s_defaultTimeout = AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeout ? timeout : Regex.InfiniteMatchTimeout;
        
        /// <summary>Whether <see cref="s_defaultTimeout"/> is non-infinite.</summary>
        internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;
        
        /// <summary>Finds the next index of any character that matches a whitespace character.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyWhiteSpace(this ReadOnlySpan<char> span)
        {
            int i = span.IndexOfAnyExcept(Utilities.s_asciiExceptWhiteSpace);
            if ((uint)i < (uint)span.Length)
            {
                if (char.IsAscii(span[i]))
                {
                    return i;
                }
        
                do
                {
                    if (char.IsWhiteSpace(span[i]))
                    {
                        return i;
                    }
                    i++;
                }
                while ((uint)i < (uint)span.Length);
            }
        
            return -1;
        }
        
        /// <summary>Supports searching for characters in or not in "\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f!\"#$%&amp;'()*+,-./0123456789:;&lt;=&gt;?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f".</summary>
        internal static readonly SearchValues<char> s_asciiExceptWhiteSpace = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f");
    }
}
