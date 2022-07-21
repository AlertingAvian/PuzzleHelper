from yaml import load, dump, CDumper as Dumper
from yaml.loader import SafeLoader
from os.path import exists
import argparse

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


test = [{'Name': 'PuzzleHelper', 'Version': '0.0.1', 'DLL': 'bin/Debug/PuzzleHelper.dll', 'Dependencies': [{'Name': 'Everest', 'Verison': '1.3366.0'}]}]
# read current everest.yaml/yml
with open(yaml_file, "r") as file:
	data = load(file, Loader=SafeLoader)

# checks
if args.version:
	spl_version = args.version.split('.')
	if len(spl_version) != 3:
		raise ValueError(f'Incorrect version format. Must be X.X.X (MAJOR.Minor.patch) not {args.version}')

if args.release:
	if not args.version:
		raise Exception('Missing version. Version is required for release.')

# Edit yaml
if args.release:
	if data[0]['Name'] == name:
		data[0]['Version'] = args.version
		if args.dll:
			data[0]['DLL'] = args.dll
	else:
		raise Exception(f"Something broke. \n{name} not found at data[0]['Name'] \ndata[0]['Name'] = {data[0]['Name']} \n{data=}")
else:
	if data[0]['Name'] == name:
		patch_version = int(data[0]['Version'].split('.')[2])
		patch_version += 1
		data[0]['Version'] = '.'.join(data[0]['Version'].split('.')[0:2]) + '.' + str(patch_version)
		if args.dll:
			data[0]['DLL'] = args.dll
	else:
		raise Exception(f"Something broke. \n{name} not found at data[0]['Name'] \ndata[0]['Name'] = {data[0]['Name']} \n{data=}")


# write everest.yaml
with open(yaml_file, "w") as file:
	output = dump(data,Dumper=Dumper, default_flow_style=False, sort_keys=False)
	file.write(output)
