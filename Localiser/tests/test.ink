INCLUDE tests/included1.ink
VAR something = "fff"

-> Intro

// How does it cope with comments?
== Intro
A normal line. #loc:test_Intro_7N83 // What about this comment?
*[Test1 #loc:test_Intro_KNIN #tagged]
    ~something = "HelloInAVar."
    Something inside a choice is this hard. #loc:test_Intro_2AFA
*[Test2 #loc:test_Intro_AB0U #tagish]
    Buckles. #loc:test_Intro_WJ1D
    Something else inside a choice2. #loc:test_Intro_E6Y3
    Booyah. #loc:test_Intro_ZZOT
-
Carry on. #loc:test_Intro_DTJ6

Let's do some branching - "do quotes and, commas work?" ("" ,,): #loc:test_Intro_CXVI
-> Branch

= Branch
Here's a branch. #loc:test_Intro_Branch_P9Q5 #tagBranch // Or this comment?

Some var \{something\} work for errors? #loc:test_Intro_Branch_JRHG

~temp tempVar = 10
~ tempVar = tempVar+1

{tempVar:
-0: Hello. #tagHello #Set of tags #loc:fred #tagBum
-1: One. #loc:test_Intro_Branch_3R1Y
-3: Three. #loc:test_Intro_Branch_L3M2
-else:
    Something else. #loc:test_Intro_Branch_TU9M
}

{tempVar>0:
    Greater than 0! #loc:test_Intro_Branch_2MRX #tagGreater
    -> Ending1
- else:
    Less than or equals 0! #loc:test_Intro_Branch_CETA #tagLesser
    -> Ending2
}

== Ending1
This is ending 1. #loc:test_Ending1_KRRF
->END

==Ending2
This is ending 2. #loc:test_Ending2_LJEV
->END