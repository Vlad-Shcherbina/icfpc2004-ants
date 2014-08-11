import re
import StringIO
import collections
import copy

import cfg_builder


class AntBuilder(cfg_builder.CfgBuilder):
    def faked_globals(self):

        def command(template):
            label = self.get_simple_label(skip=1)
            alternatives = {}
            for m in re.finditer(r'\{(\w+)\}', template):
                alternatives[m.group(1)] = (label, m.group(1))
            self.add_statement(template)
            return self.branch(alternatives)

        return dict(
            command=command)

    def minimize_automaton(self):
        partition = {k : None for k in self.basic_blocks}
        bbs = self.reverse_postorder()
        while True:
            q = collections.defaultdict(list)
            for bb in bbs:
                signature = (
                    bb.statements[0],
                    tuple((k, partition[v.label])
                          for k, v in sorted(bb.next.items())))
                q[signature].append(bb.label)
            new_partition = {label: k for k, v in q.items() for label in v}
            if len(set(new_partition.values())) == len(set(partition.values())):
                break
            partition = new_partition
        self.basic_blocks = {}
        for bb in bbs:
            if partition[bb.label] in self.basic_blocks:
                continue
            new_block = copy.copy(bb)
            new_block.label = partition[bb.label]
            self.basic_blocks[new_block.label] = new_block

        for bb in self.basic_blocks.values():
            bb.next = {
                k : self.basic_blocks[partition[v.label]]
                for k, v in bb.next.items()}

        self.begin = self.basic_blocks[partition[self.BEGIN_LABEL]]

    def get_automaton(self):
        out = StringIO.StringIO()

        bbs = self.reverse_postorder()
        index_by_label = {bb.label : i for i, bb in enumerate(bbs)}
        for i, bb in enumerate(bbs):
            [s] = bb.statements
            cmd = s.format(
                **{k : index_by_label[v.label] for k, v in bb.next.items()})
            print>>out, '{:40}; {}'.format(cmd, i)

        return out.getvalue()


def main():
    ab = AntBuilder()

    def run():
        command('turn right {next}')
        while True:
            command('turn left {next}')
            command('turn right {next}')

    ab.explore(run)
    ab.minimize_automaton()
    print ab.get_automaton()



if __name__ == '__main__':
    main()
