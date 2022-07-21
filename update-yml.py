from yaml import load, dump, CLoader as Loader, CDumper as Dumper
import sys

helper = "PuzzleHelper"
yaml_file = "everest.yaml"
release_tag = "release"

args = sys.argv[1:]

fileIn = open(yaml_file, "r")
data = load(fileIn,Loader=Loader)
fileIn.close()

for dep in data:
	if dep["Name"] == helper:
		# version bump
		vers = dep["Version"].split('.')
		# if argument passed
		new_vers = [int(vers[0]),int(vers[1]),int(vers[2])]
		if len(args) == 1:
			if args[0] == release_tag:
				new_vers[1] += 1
				new_vers[2] = 0
			else:
				new_vers[2] += 1
		else:
			new_vers[2] += 1
		dep["Version"] = str(new_vers[0]) + "." + str(new_vers[1]) + "." + str(new_vers[2])
		
output = dump(data,Dumper=Dumper)
fileOut = open(yaml_file, "w")
fileOut.write(output)
fileOut.close()