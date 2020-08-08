with open('regex.txt', 'r') as regex_file:
    with open('regexes_code.txt', 'w') as code_file:
        for line in regex_file:
            real, decoy = line.strip().split('\t')
            code_file.write(
                'new RawRegexData {{ real = "{}", decoy = "{}" }},\n'
                .format(real, decoy)
            )

with open('words.txt', 'r') as words_file:
    with open('words_code.txt', 'w') as code_file:
        for line in words_file:
            code_file.write('"{}",\n'.format(line.strip()))
