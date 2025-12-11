INCLUDE included1.ink
VAR something = "fff"

Top level? #id:test_7F18

-> Intro

// How does it cope with comments?
== Intro
A normal line. #id:test_Intro_VDOT // What about this comment?
*[Test1 #id:test_Intro_8DXY #tagged]
    ~something = "HelloInAVar."
    Something inside a choice is this hard. #id:test_Intro_UMEL
*[Test2 #id:test_Intro_6H22 #tagish]
    Buckles. #id:test_Intro_WMFS
    Something else inside a choice2. #id:test_Intro_3F7H
    Booyah. #id:test_Intro_NSPN
-
Carry on. #id:test_Intro_N9TG

{true: Does it work inside brackets? #id:test_Intro_X726}
Let's do some branching - "do quotes and, commas work?" ("" ,,): #id:test_Intro_50E9
-> Branch

= Branch
Here's a branch. #id:test_Intro_Branch_JBW0 #tagBranch // Or this comment?

Some var \{something\} work for errors? #id:test_Intro_Branch_EQKF

~temp tempVar = 10
~ tempVar = tempVar+1

{tempVar:
-0: Hello. #id:test_Intro_Branch_KXEF #tagHello #Set of tags #tagBum
-1: One. #id:test_Intro_Branch_1BE6
-3: Three. #id:test_Intro_Branch_POMK
-else:
    Something else. #id:test_Intro_Branch_0ZJB
}

{tempVar>0:
    Greater than 0! #id:test_Intro_Branch_RMBW #tagGreater
    -> Ending1
- else:
    Less than or equals 0! #id:test_Intro_Branch_CLCX #tagLesser
    -> Ending2
}

== Ending1
This is ending 1. #id:test_Ending1_EJDE
->END

==Ending2
This is ending 2. #id:test_Ending2_SLIC
->END