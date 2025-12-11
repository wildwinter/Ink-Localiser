INCLUDE included1.ink
VAR something = "fff"

Top level? #id:test_8JO4

-> Intro

// How does it cope with comments?
== Intro
A normal line. #id:test_Intro_OEL9 // What about this comment?
*[Test1 #id:test_Intro_UPJP #tagged]
    ~something = "HelloInAVar."
    Something inside a choice is this hard. #id:test_Intro_18QU
*[Test2 #id:test_Intro_ZGD6 #tagish]
    Buckles. #id:test_Intro_SHMV
    Something else inside a choice2. #id:test_Intro_FM49
    Booyah. #id:test_Intro_VYDK
-
Carry on. #id:test_Intro_VK5H

{true: Does it work inside brackets? #id:test_Intro_7XTJ}
Let's do some branching - "do quotes and, commas work?" ("" ,,): #id:test_Intro_PA4L
-> Branch

= Branch
Here's a branch. #id:test_Intro_Branch_B33J #tagBranch // Or this comment?

Some var \{something\} work for errors? #id:test_Intro_Branch_I145

~temp tempVar = 10
~ tempVar = tempVar+1

{tempVar:
-0: Hello. #id:test_Intro_Branch_SBJ5 #tagHello #Set of tags #tagBum
-1: One. #id:test_Intro_Branch_Z8WN
-3: Three. #id:test_Intro_Branch_SI6F
-else:
    Something else. #id:test_Intro_Branch_5LTI
}

{tempVar>0:
    Greater than 0! #id:test_Intro_Branch_C2IO #tagGreater
    -> Ending1
- else:
    Less than or equals 0! #id:test_Intro_Branch_XJHK #tagLesser
    -> Ending2
}

== Ending1
This is ending 1. #id:test_Ending1_65WX
->END

==Ending2
This is ending 2. #id:test_Ending2_4WS7
->END