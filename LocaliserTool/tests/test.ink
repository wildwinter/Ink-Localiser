INCLUDE included1.ink
VAR something = "fff"

Top level? #id:test_UZ4B

-> Intro

// How does it cope with comments?
== Intro
A normal line. #id:test_Intro_HXPR // What about this comment?
*[Test1 #id:test_Intro_FH00 #tagged]
    ~something = "HelloInAVar."
    Something inside a choice is this hard. #id:test_Intro_6N2J
*[Test2 #id:test_Intro_SAHM #tagish]
    Buckles. #id:test_Intro_RHRZ
    Something else inside a choice2. #id:test_Intro_0IXT
    Booyah. #id:test_Intro_BTD3
-
Carry on. #id:test_Intro_5SDF

{true: Does it work inside brackets? #id:test_Intro_02F6}
Let's do some branching - "do quotes and, commas work?" ("" ,,): #id:test_Intro_NHZE
-> Branch

= Branch
Here's a branch. #id:test_Intro_Branch_K424 #tagBranch // Or this comment?

Some var \{something\} work for errors? #id:test_Intro_Branch_V3CW

~temp tempVar = 10
~ tempVar = tempVar+1

{tempVar:
-0: Hello. #id:test_Intro_Branch_FQME #tagHello #Set of tags #tagBum
-1: One. #id:test_Intro_Branch_84DD
-3: Three. #id:test_Intro_Branch_GPN7
-else:
    Something else. #id:test_Intro_Branch_2ISF
}

{tempVar>0:
    Greater than 0! #id:test_Intro_Branch_549R #tagGreater
    -> Ending1
- else:
    Less than or equals 0! #id:test_Intro_Branch_9LGB #tagLesser
    -> Ending2
}

== Ending1
This is ending 1. #id:test_Ending1_VG9V
->END

==Ending2
This is ending 2. #id:test_Ending2_4DCP
->END