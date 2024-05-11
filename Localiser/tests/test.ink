VAR something = ""

-> Intro

== Intro
A normal line.
*[Test1 #tagged]
    ~something = "HelloInAVar."
    Something inside a choice.
*[Test2]
    Something else inside a choice.
-
Carry on.

Let's do some branching: 
-> Branch

= Branch
Here's a branch. #tagBranch

Some var \{something\} work for errors?

~temp tempVar = 10
~ tempVar = tempVar+1

{tempVar:
-0: Hello. #tagHello
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