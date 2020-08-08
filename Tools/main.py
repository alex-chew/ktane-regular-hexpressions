import re
import itertools
from collections import namedtuple, Counter, defaultdict
import random


# import generate_regexes
with open('regex.txt', 'r') as f:
    REGEXES_RAW = [tuple(l.strip().split()) for l in f if l]
REGEXES = [
    (re.compile('^{}$'.format(real)), re.compile('^{}$'.format(decoy)))
    for real, decoy in REGEXES_RAW
]

GRID = [None] * len(REGEXES)
for i in range(len(REGEXES)):
    GRID[(i * 17 + 3) % len(REGEXES)] = i
COLS = 6
ROWS = len(GRID) // COLS


def main():
    with open('words.txt') as f:
        words = [line.strip() for line in f]
    print('{} words, {} regexes'.format(len(words), len(REGEXES)))
    # analyze_matches(words)
    print_grid()
    # run_all_sims(words)

    with open('grid.html', 'w') as f:
        f.write(dump_grid_as_html())


def analyze_matches(words):
    matches = [
        (r.pattern[1:-1], w)
        for r, w in itertools.product(REGEXES, words)
        if r.match(w)
    ]

    word_matches = Counter(w for (_, w) in matches)
    regex_matches = Counter(r for (r, _) in matches)
    print('{} words have a matching regex'.format(len(word_matches)))
    print('{} regexes have matching words'.format(len(regex_matches)))

    print('Word matches:', word_matches)
    print('Regex matches:', regex_matches)
    for regex, count in regex_matches.items():
        if count < 12:
            print(regex, count)


def run_all_sims(words):
    rowcols = itertools.product(
        itertools.combinations(range(ROWS), 2),
        itertools.combinations(range(COLS), 2),
    )
    sim_passes = 0
    for rows, cols in rowcols:
        reals = [
            real_at(row, col)
            for row, col in itertools.product(rows, cols)
        ]
        decoys = [
            decoy_at(row, col)
            for row, col in itertools.product(rows, cols)
        ]
        if all((
            has_puzzle_with_unique_solution(words, reals),
            has_decoys(words, reals, decoys),
        )):
            sim_passes += 1
    
    total_sims = (ROWS * (ROWS - 1) // 2) * (COLS * (COLS - 1) // 2)
    print('{} sim fails in {} sims'.format(total_sims - sim_passes, total_sims))


def has_puzzle_with_unique_solution(words, regexes):
    """
    Check whether the given regexes admit a puzzle with a unique solution.
    That is, there exists a subset W of words
    and a *unique* bijection f between regexes and W such that:
    
        forall r in regexes. r matches f(r)
    """

    word_matches = [0b0] * len(words)
    regex_match_counts = {r: 0 for r in regexes}
    for (wi, w), (ri, r) in itertools.product(enumerate(words), enumerate(regexes)):
        if r.match(w):
            word_matches[wi] |= 0b1 << ri
            regex_match_counts[r] += 1
    remaining_ris = sorted(range(len(regexes)), key=lambda ri: regex_match_counts[regexes[ri]])

    match_flags = 0b0
    target_match_flags = (0b1 << len(regexes)) - 1
    debug_info = []
    while remaining_ris:
        matched_ri = None
        # add_choices = []
        for ri in remaining_ris:
            for wi, wm in enumerate(word_matches):
                ri_flag = 1 << ri
                # If wm matches this regex, and other wm matches overlap with existing matches,
                # then we found a new match
                if wm & ri_flag and bit_subset(wm & ~ri_flag, match_flags):
                    matched_ri = ri
                    # add_choices.append((ri, wi))
            # Only accept choices from a single regex
            if matched_ri:
                break
        
        if matched_ri is not None:
            match_flags |= 1 << matched_ri
            remaining_ris.remove(matched_ri)
            debug_info.append('matched regex #{}'.format(matched_ri))
        else:
            # Can't match on any more regexes. Fail!
            print('FAIL')
            print('regexes: {}'.format([r.pattern[1:-1] for r in regexes]))
            print(', '.join(debug_info))
            return False
    assert not remaining_ris
    return True


def has_decoys(words, reals, decoys):
    """
    Check whether there exists at least 8 words which both
    match some decoy regex and don't match any real regex.
    """
    candidates = set()
    for d in decoys:
        for w in words:
            if any(r.match(w) for r in reals):
                continue
            if d.match(w):
                candidates.add(w)
    if len(candidates) < 8:
        print('reals:', ', '.join(r.pattern[1:-1] for r in reals))
        print('decoys:', ', '.join(d.pattern[1:-1] for d in decoys))
        print('candidates:', ', '.join(candidates))
        return False
    return True
                

# the 1-bits of `a` are a subset of the 1-bits of `b`
# if `a BITCLEAR b` is 0
def bit_subset(a, b):
    return not a & ~b


def print_grid():
    for i, gi in enumerate(GRID):
        regex = REGEXES[gi][0].pattern[1:-1]
        print('{: >20} | '.format(regex), end='')
        if i % COLS == COLS - 1:
            print()


def dump_grid_as_html():
    header = '<thead>\n<tr><th></th>\n{}</tr>\n</thead>'.format(''.join(
        '<th>{}</th>\n'.format(col) for col in range(COLS)
    ))
    body = '<tbody>\n'
    for row in range(ROWS):
        body += '<tr>\n<td>{}</td>\n'.format(row)
        for col in range(COLS):
            regex = REGEXES_RAW[GRID[row * COLS + col]][0]
            body += '<td>{}</td>\n'.format(regex)
        body += '</tr>\n'
    body += '</tbody>'
    return '<table>\n{}\n{}\n</table>\n'.format(header, body)


def real_at(row, col):
    return REGEXES[GRID[row * COLS + col]][0]


def decoy_at(row, col):
    return REGEXES[GRID[row * COLS + col]][1]


if __name__ == '__main__':
    main()
