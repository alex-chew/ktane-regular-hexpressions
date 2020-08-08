import re
import itertools
from collections import namedtuple

REGEXES = []
for a in 'ADHWU':
    REGEXES.append((
        '{}.*'.format(a),  # initial letter
        '[^{0}].*{0}.*'.format(a),  # non-initial letter
    ))
for a in 'EHPYO':
    REGEXES.append((
        '.*{}'.format(a),  # terminal letter
        '.*{0}.*[^{0}]'.format(a),  # non-terminal letter
    ))
REGEXES.append((
    '.*[AEIOU][AEIOU].*',  # consecutive vowels
    '.*[AEIOU].*[AEIOU].*',  # non-consecutive vowels (assuming not real match)
))
REGEXES.append((
    '.*[AEIOU].*[AEIOU].*',  # at least two vowels
    '.*[AEIOU].*',  # just one vowel (assuming not real match)
))
REGEXES.append((
    '.*[AEIOU]',  # terminal vowel
    '.*[AEIOU].*[^AEIOU]',  # non-terminal vowel
))
REGEXES.append((
    '.*[AEIOU].[AEIOU].*',  # vowels with something between
    '.*[AEIOU][AEIOU].*',  # consecutive vowels
))
REGEXES.append((
    '.*[^AEIOU][^AEIOU].*',  # consecutive consonants
    '.*[^AEIOU].*[^AEIOU].*',  # non-consecutive consonants (assuming not real match)
))
REGEXES.append((
    '.*[^AEIOU][^AEIOU].',  # non-terminal consecutive consonants
    '.*[^AEIOU][^AEIOU]',  # terminal consecutive consonants
))
REGEXES.append((
    '[^AEIOU][^AEIOU].*',  # initial consecutive consonants
    '[^AEIOU][AEIOU].*',  # initial consonant-vowel pair
))
for a, b in 'OE IT BT FR OH FT'.split():
    REGEXES.append((
        '.*{}.*{}.*'.format(a, b),  # a,b subsequence
        '.*{}.*{}.*'.format(b, a),  # b,a subsequence
    ))
for a, b in 'OW ON OR OT HT TH DI'.split():
    REGEXES.append((
        '.*{}{}.*'.format(a, b),  # ab adjacent
        '.*{}.+{}.*'.format(a, b),  # a,b non-adjacent subsequence
    ))
for a, b in 'OW NO OT HT ER FI'.split():
    REGEXES.append((
        '.*({0}{1}|{1}{0}).*'.format(a, b),  # ab or ba adjacent
        '.*({0}.+{1}|{1}.+{0}).*'.format(a, b),  # a and b, but not adjacent
    ))
for i in range(2, 7):
    REGEXES.append((
        '.' * i,  # length i
        '.' * (i - 1) + '(..)?',  # length i-1 or i+1
    ))
for i in range(2, 6):
    REGEXES.append((
        '.' * i + '?',  # length i-1 or i
        '.' * (i + 1),  # length i+1
    ))
REGEXES.append((
    '(..)?.',  # length 1 or 3
    '(..)?..',  # length 2 or 4
))
for a, b in 'OT MO UG OW EA RE FO BO IN'.split():
    REGEXES.append((
        '.*{}[^{}].*'.format(a, b),  # a followed by not-b
        '.*{}.*{}.*'.format(a, b),  # a,b subsequence
    ))
for a, b in 'ON OT HT EA ST'.split():
    REGEXES.append((
        '.*[^{}]{}.*'.format(a, b),  # b preceded by not-a
        '.*{}.*{}.*'.format(a, b),  # a,b subsequence
    ))
for a, b, c, d in 'OUMN BTOE'.split():
    REGEXES.append((
        '.*[{}{}][{}{}].*'.format(a, b, c, d),  # (a or b) followed by (c or d)
        # [ab][ab] or [cd][cd] or [cd][ab] (assuming not real match)
        '.*[{a}{b}{c}{d}][{a}{b}{c}{d}].*'.format(
            a=a, b=b, c=c, d=d),
    ))
REGEXES.append((
    '.*L[AEIOU].*',  # L followed by vowel
    '.*L[^AEIOU]?.*',  # L not followed by vowel
))
REGEXES.append((
    '.[AEIOU].',  # three letters with middle vowel
    '.(.[AEIOU]|[AEIOU].).',  # four letters with middle vowel
))
REGEXES.append((
    '.*W(HA|HI|A).*',  # whatever the heck this is
    '.*W.*',  # has W, but not preceding HA|HI|A (assuming not real match)
))
REGEXES.append((
    '.*OL?[DT].*',  # ...(OD|OLD|OT|OLT)...
    '.*[OLDT].*[OLDT].*',  # has two [OLDT], but not real match
))
for s in 'STRAIGHT QUESTION ASTERISK BRACKET REFUTE ELIMINATE'.split():
    REGEXES.append((
        '[^{}]+'.format(s),  # none of word letters
        '[^{0}]*[{0}][^{0}]*'.format(s),  # exactly one of word letters
    ))

print('Writing regexes')
with open('regex.txt', 'w') as f:
    for real, decoy in REGEXES:
        f.write('{}\t{}\n'.format(real, decoy))