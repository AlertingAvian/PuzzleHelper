from yaml import load, dump
import yaml
from yaml.loader import SafeLoader
import argparse

# Workaround for correct list indenting (https://github.com/yaml/pyyaml/issues/234) (https://github.com/yaml/pyyaml/issues/234#issuecomment-765894586)
class Dumper(yaml.Dumper):
	def increase_indent(self, flow=False, *args, **kwargs):
		return super().increase_indent(flow=flow, indentless=False)


# setup argument parser
parser = argparse.ArgumentParser()
parser.add_argument("-v", "--version", dest="version", help="Version", type=str)
parser.add_argument("-r", "--release", dest="release", action='store_true', help="If version is a release")
parser.add_argument("-dll", dest="dll", help="Location of the mod dll, releative to mod root", type=str)

args = parser.parse_args()


# Default values
name = "PuzzleHelper"
yaml_file = "everest.yaml"
release_tag = "release"


# read current everest.yaml/yml
with open(yaml_file, "r") as file:
	data = load(file, Loader=SafeLoader)

# checks
if args.version:
	spl_version = args.version.split('.')
	if len(spl_version) != 3:
		raise ValueError(f'Incorrect version format. Must be X.X.X (MAJOR.Minor.patch) not {args.version}')
	version = ""
	version += "".join(filter(str.isdigit, spl_version[0])) + "."
	version += "".join(filter(str.isdigit, spl_version[1])) + "."
	last_numeric = 0
	for i, char in enumerate(spl_version[2]):
		if char.isdigit():
			last_numeric = i
		else:
			break
	version += spl_version[2][:last_numeric+1]

if args.release:
	if not args.version:
		raise Exception('Missing version. Version is required for release.')

# Edit yaml
if args.release:
	if data[0]['Name'] == name:
		data[0]['Version'] = version
		if args.dll:
			data[0]['DLL'] = args.dll
	else:
		raise Exception(f"Something broke. \n{name} not found at data[0]['Name'] \ndata[0]['Name'] = {data[0]['Name']} \n{data=}")
elif args.version and not args.release and not args.dll:
	data[0]['Version'] = version
else:
	raise Exception("Missing arguments")

print(data)

# write everest.yaml
with open(yaml_file, "w") as file:
	output = dump(data, Dumper=Dumper, default_flow_style=False, sort_keys=False)
	file.write(output)
