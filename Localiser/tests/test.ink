INCLUDE tests/included1.ink
VAR something = "fff"

-> Intro

// How does it cope with comments?
== Intro
A normal line. // What about this comment?
*[Test1 #tagged]
    ~something = "HelloInAVar."
    Something inside a choice.
*[Test2 #tagish]
    Something else inside a choice.
-
Carry on.

Let's do some branching - "do quotes and, commas work?" ("" ,,): 
-> Branch

= Branch
Here's a branch. #tagBranch // Or this comment?

Some var \{something\} work for errors?

~temp tempVar = 10
~ tempVar = tempVar+1

{tempVar:
-0: Hello. #tagHello #Set of tags #loc:fred #tagBum
-1: One.
-3: Three.
-else:
    Something else.
}

{tempVar>0:
    Greater than 0! #tagGreater
    -> Ending1
- else:
    Less than or equals 0! #tagLesser
    -> Ending2
}

== Ending1
This is ending 1.
->END

==Ending2
This is ending 2.
->END