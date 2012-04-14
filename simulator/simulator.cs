
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;

namespace simulator
{
    public enum CellType
    {
        EMPTY, ROCK, FOOD, RED_ANTHILL, BLACK_ANTHILL
    }

    public class Cell
    {
        public CellType type;
        public int food;
        public Ant ant; // or null
        public int[] markers = new int[] { 0, 0 };

        public bool updated = true; // for rendering optimization

        public override string ToString()
        {
            if (type == CellType.ROCK)
                return "rock";
            string s = "";
            if (food > 0)
                s += String.Format("{0} food; ", food);
            if (type == CellType.RED_ANTHILL)
                s += "red hill; ";
            else if (type == CellType.BLACK_ANTHILL)
                s += "black hill; ";
            if (markers[0] > 0)
            {
                s += "red marks: ";
                for (int i = 0; i < 6; i++)
                    if ((markers[0] & (1 << i)) != 0)
                        s += i.ToString();
                s += "; ";
            }
            if (markers[1] > 0)
            {
                s += "black marks: ";
                for (int i = 0; i < 6; i++)
                    if ((markers[1] & (1 << i)) != 0)
                        s += i.ToString();
                s += "; ";
            }
            if (ant != null)
            {
                s += String.Format("{0} ant of id {1}, dir {2}, food {3}, state {4}, resting {5}",
                        new String[] { "red", "black" }[ant.color],
                        ant.id, ant.direction, ant.hasFood ? 1 : 0, ant.state, ant.resting);
            }
            return s;
        }
    }

    public class Map
    {
        public static int[,] directions =
            new int[,] { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 } };
        public int width, height;

        public Cell[,] cells;

        public Map(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            StreamReader r = new StreamReader(fs);
            width = Int32.Parse(r.ReadLine());
            height = Int32.Parse(r.ReadLine());
            cells = new Cell[width, height];
            for (int j = 0; j < height; j++)
            {
                string[] elems =
                    r.ReadLine().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < width; i++)
                {
                    cells[i, j] = new Cell();
                    if (elems[i] == ".")
                        cells[i, j].type = CellType.EMPTY;
                    else if (elems[i] == "#")
                        cells[i, j].type = CellType.ROCK;
                    else if (Char.IsDigit(elems[i][0]))
                    {
                        cells[i, j].type = CellType.EMPTY;
                        cells[i, j].food = Int32.Parse(elems[i]);
                    }
                    else if (elems[i] == "+")
                        cells[i, j].type = CellType.RED_ANTHILL;
                    else if (elems[i] == "-")
                        cells[i, j].type = CellType.BLACK_ANTHILL;
                }
            }
            r.Close();
            transformToInternalForm();
        }

        private void transformToInternalForm()
        {
            Cell[,] tmp = cells;
            cells = new Cell[width + height / 2, height];
            for (int i = 0; i < cells.GetLength(0); i++)
                for (int j = 0; j < cells.GetLength(1); j++)
                    cells[i, j] = new Cell();

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    cells[i + (j + 1) / 2, j] = tmp[i, j];
            width = width + (height + 1) / 2;
        }
    }

    public class Ant
    {
        public int id;
        public int color; // 0 - red, 1 - black
        public int state;
        public int resting;
        public bool hasFood;
        public int x, y;
        public int direction;
        public Ant(int id, int x, int y, int color)
        {
            this.id = id;
            this.color = color;
            state = 0;
            resting = 0;
            hasFood = false;
            this.x = x;
            this.y = y;
            direction = 0;
        }
    }

    public abstract class Instruction
    {
        public string comment = "";
        public virtual void execute(Ant ant, Simulator simulator)
        {
        }
    }

    public class Sense : Instruction
    {
        public Dir dir;
        public int trueState, falseState;
        public Cond cond;

        public enum Dir
        {
            Here, Ahead, LeftAhead, RightAhead
        }
        public enum Cond
        {
            Friend, Foe, FriendWithFood, FoeWithFood, Food, Rock, Home, FoeHome, FoeMarker,
            Marker // it is used as Marker+bit
        }
        public Sense(string[] elems)
        {
            Debug.Assert(elems.Length >= 4);
            string d = elems[1].ToLower();
            if (d == "here")
                dir = Dir.Here;
            else if (d == "ahead")
                dir = Dir.Ahead;
            else if (d == "leftahead")
                dir = Dir.LeftAhead;
            else if (d == "rightahead")
                dir = Dir.RightAhead;
            else
                Debug.Assert(false);
            trueState = Int32.Parse(elems[2]);
            falseState = Int32.Parse(elems[3]);
            string c = elems[4].ToLower();
            if (c == "friend")
                cond = Cond.Friend;
            else if (c == "foe")
                cond = Cond.Foe;
            else if (c == "friendwithfood")
                cond = Cond.FriendWithFood;
            else if (c == "foewithfood")
                cond = Cond.FoeWithFood;
            else if (c == "food")
                cond = Cond.Food;
            else if (c == "rock")
                cond = Cond.Rock;
            else if (c == "home")
                cond = Cond.Home;
            else if (c == "foehome")
                cond = Cond.FoeHome;
            else if (c == "foemarker")
                cond = Cond.FoeMarker;
            else if (c == "marker")
                cond = Cond.Marker + Int32.Parse(elems[5]);
            else
                Debug.Assert(false);
        }
        bool cellMatches(Cell cell, int color)
        {
            if (cond == Cond.Friend)
                return cell.ant != null && cell.ant.color == color;
            else if (cond == Cond.Foe)
                return cell.ant != null && cell.ant.color != color;
            else if (cond == Cond.FriendWithFood)
                return cell.ant != null && cell.ant.color == color && cell.ant.hasFood;
            else if (cond == Cond.FoeWithFood)
                return cell.ant != null && cell.ant.color != color && cell.ant.hasFood;
            else if (cond == Cond.Food)
                return cell.food > 0;
            else if (cond == Cond.Rock)
                return cell.type == CellType.ROCK;
            else if (cond == Cond.Home)
                return color == 0 && cell.type == CellType.RED_ANTHILL ||
                       color == 1 && cell.type == CellType.BLACK_ANTHILL;
            else if (cond == Cond.FoeHome)
                return color == 1 && cell.type == CellType.RED_ANTHILL ||
                       color == 0 && cell.type == CellType.BLACK_ANTHILL;
            else if (cond == Cond.FoeMarker)
                return cell.markers[1 - color] > 0;
            else if (cond >= Cond.Marker)
                return (cell.markers[color] & (1 << (cond - Cond.Marker))) != 0;
            else
                Debug.Assert(false);
            return false;
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            int x = ant.x;
            int y = ant.y;
            if (dir == Dir.Ahead)
            {
                x += Map.directions[ant.direction, 0];
                y += Map.directions[ant.direction, 1];
            }
            else if (dir == Dir.LeftAhead)
            {
                x += Map.directions[(ant.direction + 5) % 6, 0];
                y += Map.directions[(ant.direction + 5) % 6, 1];
            }
            else if (dir == Dir.RightAhead)
            {
                x += Map.directions[(ant.direction + 1) % 6, 0];
                y += Map.directions[(ant.direction + 1) % 6, 1];
            }
            Cell cell = simulator.map.cells[x, y];
            if (cellMatches(cell, ant.color))
                ant.state = trueState;
            else
                ant.state = falseState;
        }
    }

    public class Mark : Instruction
    {
        public int nextState;
        public int bit;
        bool unMark;
        public Mark(string[] elems)
        {
            unMark = elems[0] == "unmark";
            Debug.Assert(elems.Length == 3);
            bit = Int32.Parse(elems[1]);
            Debug.Assert(bit >= 0 && bit < 6);
            nextState = Int32.Parse(elems[2]);
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            Cell cell = simulator.map.cells[ant.x, ant.y];
            if (unMark)
                cell.markers[ant.color] &= ~(1 << bit);
            else
                cell.markers[ant.color] |= 1 << bit;
            cell.updated = true;
            ant.state = nextState;
        }
    }

    public class PickUp : Instruction
    {
        public int nextState, failState;
        public PickUp(string[] elems)
        {
            Debug.Assert(elems.Length == 3);
            nextState = Int32.Parse(elems[1]);
            failState = Int32.Parse(elems[2]);
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            Cell cell = simulator.map.cells[ant.x, ant.y];
            if (ant.hasFood || cell.food == 0)
                ant.state = failState;
            else
            {
                cell.food--;
                cell.updated = true;
                ant.hasFood = true;
                ant.state = nextState;
            }
        }
    }

    public class Drop : Instruction
    {
        public int nextState;
        public Drop(string[] elems)
        {
            Debug.Assert(elems.Length == 2);
            nextState = Int32.Parse(elems[1]);
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            if (ant.hasFood)
            {
                ant.hasFood = false;
                Cell cell = simulator.map.cells[ant.x, ant.y];
                cell.food++;
                cell.updated = true;
            }
            ant.state = nextState;
        }
    }

    public class Turn : Instruction
    {
        public int nextState, dDir;
        public Turn(string[] elems)
        {
            Debug.Assert(elems.Length == 3);
            if (elems[1].ToLower() == "left")
                dDir = -1;
            else if (elems[1].ToLower() == "right")
                dDir = 1;
            else
                Debug.Assert(false);
            nextState = Int32.Parse(elems[2]);
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            ant.direction += dDir;
            ant.direction = (ant.direction + 6) % 6;
            ant.state = nextState;
            simulator.map.cells[ant.x, ant.y].updated = true;
        }
    }

    public class Move : Instruction
    {
        public int nextState, failState;
        public Move(string[] elems)
        {
            Debug.Assert(elems.Length == 3);
            nextState = Int32.Parse(elems[1]);
            failState = Int32.Parse(elems[2]);
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            int newX = ant.x + Map.directions[ant.direction, 0];
            int newY = ant.y + Map.directions[ant.direction, 1];
            Cell oldCell = simulator.map.cells[ant.x, ant.y];
            Debug.Assert(oldCell.ant == ant);
            Cell newCell = simulator.map.cells[newX, newY];
            if (newCell.type == CellType.ROCK || newCell.ant != null)
                ant.state = failState;
            else
            {
                oldCell.updated = true;
                newCell.updated = true;
                oldCell.ant = null;
                newCell.ant = ant;
                ant.x = newX;
                ant.y = newY;
                ant.resting = 14;
                ant.state = nextState;
                simulator.checkForSurroundedAnts(ant.x, ant.y);
            }
        }
    }

    public class Flip : Instruction
    {
        public int p;
        public int nextState1, nextState2;
        public Flip(string[] elems)
        {
            Debug.Assert(elems.Length == 4);
            p = Int32.Parse(elems[1]);
            nextState1 = Int32.Parse(elems[2]);
            nextState2 = Int32.Parse(elems[3]);
        }
        public override void execute(Ant ant, Simulator simulator)
        {
            if (simulator.random(p) == 0)
                ant.state = nextState1;
            else
                ant.state = nextState2;
        }
    }

    public class Automaton
    {
        public Instruction[] instructions;
        public Automaton(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            StreamReader r = new StreamReader(fs);
            List<Instruction> instrs = new List<Instruction>();
            while (true)
            {
                string line = r.ReadLine();
                if (line == null)
                    break;
                line = line.Trim();
                if (line == "")
                    break;
                int commentStart = line.IndexOf(";");
                string comment = "";
                if (commentStart != -1)
                {
                    comment = line.Substring(commentStart);
                    line = line.Substring(0, commentStart);
                }
                string[] elems = line.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                elems[0] = elems[0].ToLower();
                Instruction cmd = null;
                if (elems[0] == "sense")
                    cmd = new Sense(elems);
                else if (elems[0] == "mark" || elems[0] == "unmark")
                    cmd = new Mark(elems);
                else if (elems[0] == "pickup")
                    cmd = new PickUp(elems);
                else if (elems[0] == "drop")
                    cmd = new Drop(elems);
                else if (elems[0] == "turn")
                    cmd = new Turn(elems);
                else if (elems[0] == "move")
                    cmd = new Move(elems);
                else if (elems[0] == "flip")
                    cmd = new Flip(elems);
                else
                    Debug.Assert(false);
                cmd.comment = comment;
                instrs.Add(cmd);
            }
            instructions = instrs.ToArray();
            r.Close();
        }
    }

    public class Simulator
    {
        public Map map;
        public Automaton[] programs;
        public List<Ant> ants = new List<Ant>();

        public uint seed;
        public int round = 0;

        public int random(int n)
        {
            int res = (int)((seed >> 16) & 16383);
            seed = seed * 22695477 + 1;
            return res % n;
        }

        public Simulator(Map map, Automaton redProgram, Automaton blackProgram, int seed)
        {
            this.seed = (uint)seed;
            for (int i = 0; i < 4; i++)
                random(1);

            this.map = map;
            programs = new Automaton[] { redProgram, blackProgram };
            int id = 0;
            for (int j = 0; j < map.height; j++)
                for (int i = 0; i < map.width; i++)
                {
                    if (map.cells[i, j].type == CellType.RED_ANTHILL)
                    {
                        Ant ant = new Ant(id++, i, j, 0);
                        map.cells[i, j].ant = ant;
                        ants.Add(ant);
                    }
                    else if (map.cells[i, j].type == CellType.BLACK_ANTHILL)
                    {
                        Ant ant = new Ant(id++, i, j, 1);
                        map.cells[i, j].ant = ant;
                        ants.Add(ant);
                    }
                }
        }

        void checkForSurroundedAnt(Ant ant)
        {
            if (ant == null)
                return;
            int count = 0;
            for (int i = 0; i < 6; i++)
            {
                int x = ant.x + Map.directions[i, 0];
                int y = ant.y + Map.directions[i, 1];
                Cell cell = map.cells[x, y];
                if (cell.ant != null && cell.ant.color != ant.color)
                    count++;
            }
            if (count >= 5)
            {
                Cell cell = map.cells[ant.x, ant.y];
                cell.ant = null;
                cell.food += 3;
                if (ant.hasFood)
                    cell.food++;
                cell.updated = true;
            }
        }

        public void checkForSurroundedAnts(int x, int y)
        {
            checkForSurroundedAnt(map.cells[x, y].ant);
            for (int i = 0; i < 6; i++)
            {
                int xx = x + Map.directions[i, 0];
                int yy = y + Map.directions[i, 1];
                checkForSurroundedAnt(map.cells[xx, yy].ant);
            }
        }

        public void step(Ant ant)
        {
            if (map.cells[ant.x, ant.y].ant != ant)
                return; // ant was eliminated
            if (ant.resting > 0)
                ant.resting--;
            else
                programs[ant.color].instructions[ant.state].execute(ant, this);
        }

        public void step()
        {
            round++;
            foreach (Ant ant in ants)
                step(ant);
        }

        public void dump(TextWriter wr)
        {
            wr.WriteLine();
            wr.WriteLine("After round {0}...", round);
            for (int j = 0; j < map.height; j++)
                for (int i = 0; i < map.width - map.height / 2; i++)
                {
                    int ii = i + (j + 1) / 2;
                    wr.WriteLine("cell ({0}, {1}): {2}", i, j, map.cells[ii, j]);
                }
        }

        public void printScore()
        {
            int redScore = 0;
            int blackScore = 0;
            for (int j = 0; j < map.height; j++)
                for (int i = 0; i < map.width - map.height / 2; i++)
                {
                    int ii = i + (j + 1) / 2;
                    Cell cell = map.cells[ii, j];
                    if (cell.type == CellType.RED_ANTHILL)
                        redScore += cell.food;
                    else if (cell.type == CellType.BLACK_ANTHILL)
                        blackScore += cell.food;
                }
            Console.WriteLine("Red  : {0} food", redScore);
            Console.WriteLine("Black: {0} food", blackScore);
        }
    }
}