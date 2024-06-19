# This is a comment. Everything after the # will be ignored.
# Comments can take up an entire or be placed at the end of a line.

# Assign a value to a register. 
# Registers are temporary and hold unique values per script
# Each script can access four registers $r0 to $r3
$r0 = 3 

# Assign with a math operation. 
# Valid math operations are Add (+), Subtract (-), Multiply (*), Divide (/), Modulo (%)
$r1 = $r0 + $r0

# Assign with a comparison operation
# Valid comparisons are Equal (==),  Not Equal (!=),
# Less (<), Less or equal(<=), Greater (>), Greater or Equal (>=)
# The result will either be 0 if failed, or 1 on success
$r1 = $r0 >= 5

# Assign labels and jump to these labels when needed.
Start: #This is a label that can be jumped to
j @Start # jump to label Start
jc $r1 @Start # jump to label Start if $r1 != 0

# By default, code is executed line per line. 
# Jumping to a label changes the control flow.
# This example counts from 0 to 10
Start:
$r0 = 0
Compare:
$r0 = $r0 + 1
$r1 = $r0 < 10
jc $r1 @Compare
# $r0 is 10 here
j Start # Repeat indefinitely

# Assign or read values from Parameters of an equipped item.
# Only equipments in the same inventory can be accessed,
# but some equipments may expose values from somewhere else
SignalWriter.Value = MiningDrill.Range * 4

# Note that some parameters are read-only.
# Assigning anything to a read-only parameter is an error
MiningDrill.Range = 10000 # Error

# Some parameters represent a list of values. 
MemoryChip.Data[0] = 3
MemoryChip.Data[1] = 27
$r0 = MemoryChip.Data[0] + MemoryChip.Data[1]
# $r0 is now 30

# Each list also has an associated parameter for the list's length
# Reading data beyond the length returns 0
# Writing data beyond the length does nothing
$r0 = MemoryChip.DataCount
$r0 = $r0 + 9999
MemoryChip.DataCount[$r0] = 1234
$r1 = MemoryChip.DataCount[$r0] # $r1 is 0
