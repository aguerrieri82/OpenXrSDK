#!/usr/bin/env python3
"""Flatten SFZ #include/#define directives into one standalone SFZ text file.

Audio samples are NOT copied or modified; sample paths are rewritten relative
 to the output SFZ location.
"""

import argparse
import os
import re
import sys
from pathlib import Path

INCLUDE_RE = re.compile(r'^\s*#include\s+["\']([^"\']+)["\']\s*$')
DEFINE_RE = re.compile(r'#define\s+(\$[A-Za-z0-9_]+)\s+(.+?)(?=\s+#define\b|$)')
OPCODE_RE = re.compile(r'(?<![\w])(?:sample|default_path)\s*=', re.I)
NEXT_OPCODE_RE = re.compile(r'\s+(?=[A-Za-z_][A-Za-z0-9_]*\s*=)')


def strip_comment(line):
    quote = None
    for i, char in enumerate(line):
        if char in ('"', "'"):
            quote = None if quote == char else char if quote is None else quote
        if quote is None and line[i:i+2] == '//':
            return line[:i]
    return line


class SfzFlattener:
    def __init__(self, source: Path, target: Path, strict: bool = False):
        self.source = source.resolve()
        self.target = target.resolve()
        self.strict = strict
        self.macros = {}
        self.default_path = ''
        self.inputs = set()
        self.include_count = 0
        self.sample_count = 0
        self.missing_samples = set()
        self.samples = set()
        self.lines = []
        self.stack = []

    def expand(self, text, location, visiting=()):
        """Resolve macros by longest *defined* name, including concatenations.

        E.g. $MIC_$ART_$VEL1_$RR-$CODE_01.$ext, where $MIC (not $MIC_)
        is the defined name and the underscores between macros are literal.
        """
        result = []
        i = 0
        while i < len(text):
            if text[i] != '$':
                result.append(text[i])
                i += 1
                continue

            end = i + 1
            while end < len(text) and (text[end].isalnum() or text[end] == '_'):
                end += 1

            # Longer defined names take precedence. A trailing underscore may be
            # a literal separator, as in Naked Drums' $MIC_$ART_... pattern.
            name = None
            for n in range(end, i + 1, -1):
                candidate = text[i:n]
                if candidate in self.macros:
                    name = candidate
                    break

            if name is None:
                raise ValueError(f'{location}: unresolved SFZ macro: {text[i:end]}')
            if name in visiting:
                raise ValueError(f'{location}: recursive SFZ macro: {" -> ".join((*visiting, name))}')

            result.append(self.expand(self.macros[name], location, (*visiting, name)))
            i += len(name)

        return ''.join(result)

    def normalize_sample(self, raw, location):
        raw = raw.strip().strip('"\'').replace('\\', '/')
        if not raw:
            raise ValueError(f'{location}: empty sample path')
        if raw.startswith('*'):
            return raw  # SFZ built-in oscillator/sample identifiers.

        base = self.source.parent / self.default_path.replace('\\', '/')
        sample = Path(raw)
        if not sample.is_absolute():
            sample = base / sample
        sample = sample.resolve()
        self.samples.add(sample)
        if not sample.is_file():
            self.missing_samples.add(sample)

        try:
            return os.path.relpath(sample, self.target.parent).replace('\\', '/')
        except ValueError:  # E.g. different Windows drive letters.
            return sample.as_posix()

    def rewrite_paths(self, line, location):
        matches = list(OPCODE_RE.finditer(line))
        if not matches:
            return line
        parts = []
        pos = 0
        for match in matches:
            end_match = NEXT_OPCODE_RE.search(line, match.end())
            end = end_match.start() if end_match else len(line)
            # If another path opcode appears next, don't cross into it.
            next_path = next((m for m in matches if m.start() > match.start()), None)
            if next_path is not None:
                end = min(end, next_path.start())
            value = line[match.end():end].strip()
            parts.append(line[pos:match.start()])
            if line[match.start():match.end()].strip().lower().startswith('default_path'):
                self.default_path = value.strip('"\'')
            else:
                parts.append('sample=' + self.normalize_sample(value, location))
                self.sample_count += 1
            pos = end
        parts.append(line[pos:])
        return ''.join(parts).strip()

    def process(self, file: Path):
        file = file.resolve()
        if file in self.stack:
            raise ValueError('Cyclic SFZ include: ' + ' -> '.join(str(p) for p in [*self.stack, file]))
        if not file.is_file():
            raise FileNotFoundError(f'SFZ include not found: {file}')
        self.inputs.add(file)
        self.stack.append(file)
        try:
            try:
                content = file.read_text(encoding='utf-8-sig')
            except UnicodeDecodeError:
                content = file.read_text(encoding='cp1252')

            for lineno, original in enumerate(content.splitlines(), 1):
                location = f'{file}:{lineno}'
                line = strip_comment(original).strip()
                if not line:
                    continue

                if line.startswith('#include'):
                    match = INCLUDE_RE.fullmatch(line)
                    if not match:
                        raise ValueError(f'{location}: invalid #include: {line}')
                    path = self.expand(match.group(1), location)
                    self.include_count += 1
                    include = Path(path)
                    if not include.is_absolute():
                        # SFZ includes in Naked Drums use paths relative to the
                        # entry SFZ, even inside deeply nested include files.
                        primary = self.source.parent / include
                        fallback = file.parent / include
                        include = primary if primary.is_file() or not fallback.is_file() else fallback
                    self.process(include)
                    continue

                if line.startswith('#define'):
                    definitions = list(DEFINE_RE.finditer(line))
                    if not definitions or line[:definitions[0].start()].strip():
                        raise ValueError(f'{location}: invalid #define: {line}')
                    for definition in definitions:
                        self.macros[definition.group(1)] = definition.group(2).strip()
                    continue

                if line.startswith('#undef'):
                    self.macros.pop(line.removeprefix('#undef').strip(), None)
                    continue

                if line.startswith('#'):
                    raise ValueError(f'{location}: unsupported directive: {line}')

                line = self.expand(line, location)
                line = self.rewrite_paths(line, location)
                if line:
                    self.lines.append(line)
        finally:
            self.stack.pop()

    def run(self):
        self.process(self.source)
        if self.target in self.inputs:
            raise ValueError(f'Refusing to overwrite source SFZ/include: {self.target}')
        if self.missing_samples:
            print(f'WARNING: {len(self.missing_samples)} referenced sample file(s) are missing:', file=sys.stderr)
            for path in sorted(self.missing_samples)[:20]:
                print(f'  {path}', file=sys.stderr)
            if len(self.missing_samples) > 20:
                print('  ...', file=sys.stderr)
            if self.strict:
                raise FileNotFoundError('Missing samples; output not written (--strict)')

        self.target.parent.mkdir(parents=True, exist_ok=True)
        temp = self.target.with_name(self.target.name + '.tmp')
        if temp in self.inputs:
            raise ValueError(f'Refusing to overwrite source SFZ/include: {temp}')
        try:
            temp.write_text('// Generated from ' + self.source.name + '\n' + '\n'.join(self.lines) + '\n', encoding='utf-8')
            temp.replace(self.target)
        finally:
            temp.unlink(missing_ok=True)

        print(f'Output: {self.target}')
        print(f'Includes expanded: {self.include_count}')
        print(f'Output lines: {len(self.lines)}')
        print(f'Sample references: {self.sample_count} ({len(self.samples)} unique, {len(self.missing_samples)} missing)')


def main():
    parser = argparse.ArgumentParser(description='Flatten an SFZ and its included text files, expand macros, fix sample paths.')
    parser.add_argument('input', type=Path, help='Entry SFZ file')
    parser.add_argument('output', nargs='?', type=Path, help='Output SFZ (default: <input>.flat.sfz alongside input)')
    parser.add_argument('--strict', action='store_true', help='Fail if any referenced audio sample is missing')
    args = parser.parse_args()
    target = args.output or args.input.with_name(args.input.stem + '.flat.sfz')
    try:
        SfzFlattener(args.input, target, args.strict).run()
    except (OSError, ValueError) as exc:
        print(f'ERROR: {exc}', file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
