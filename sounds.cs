// Sound Sequencer
// Use a single or a group of sound blocks to play a sequence of sounds
// Author: 123
// Version: 0.1.0
//
// Getting started:
// 1. Install this script to a Programmable Block
// 2. Via the many methods, ie Timerblocks, Sensors, Buttons, run the block with a comma delineated
// argument list beginning with the target Sound Block or Sound Block Group.
//
// NOTE: The first argument in the list must be the case-sensitive name of a Sound Block or Sound Block Group.
//
// ID Examples - Exclude the quotes. The "Name Type" tag is not case-sensitive.
// But the syntax must match one of the below examples.
// Block Example: "{B:Sound Block 1}"  -  Group Example: "{G:Sound Blocks}"
// Block Example: "{b:Sound Block 1}"  -  Group Example: "{g:Sound Blocks}"
//
// Complete Example:
// Assuming you have AQD - Computer Voice Lines installed
// https://steamcommunity.com/sharedfiles/filedetails/?id=1799565089&searchtext=aqd
// Or another sound mod, this does work with the default sounds but there aren't many.
// You can then create play sequences that will play in a single sound block.
//
// Example: "{B:Sound Block}, SoundBlockAlert1, AQD_SB_Arc_097, AQD_SB_Arc_096, AQD_SB_Arc_095"
//
// You'll notice that the alert sound is cut off and there are long pauses on the others. That will be addressed
// in the advanced usage section.
//
// Advanced Usage:
// In order to configure the timing of a sound sample, you can suffix the name of the sample with a ":" without
// the quotes, followed by the number of ticks which represents the play length of that particular sample.
// For example, below is the same example as above but configured to work with a block group and the
// individual sound sample lengths follow each sound.
//
// NOTE: A tick is not required for a single sound or the last sound in a list since it is the last sound.
// If no tick is set the default tick of "200" will be used. The duration of 1 Tick is dependent on your sim speed.
// The default can be changed in the config section below.
//
// NOTE: The following configurations are more advanced to showcase the flexibility of the script.
// {B:Sound Block - Hangar {Alert}}, SoundBlockAlert1:75, AQD_SB_Arc_097:80, AQD_SB_Arc_096:80, AQD_SB_Arc_095
// {G:Sound Blocks{Alert}}, SoundBlockAlert1:75, AQD_SB_Arc_097:80, AQD_SB_Arc_096:80, AQD_SB_Arc_095
//
// As you can see, after some playing with the numbers I've found this config to be good for this particular set
// of sounds. You will have to play with the numbers to find what is best for your setup.
// NOTE: If you set the Tick to "0" the file will not play, well it will but it'll end immediately after beginning
// which is effectively the same thing.
//
//  Deep Example:
//    "{B:Sound Block - Hangar {Alert}}, SoundBlockAlert1:75, AQD_SB_Arc_097:80, AQD_SB_Arc_096:80, AQD_SB_Arc_095"
//  will have the following results: Note: REMOVE THE QUOTES when using as an argument in the Programmable Block.
//
//  Sound SoundBlockAlert1 will play for 75 ticks
//  Sound AQD_SB_Arc_097 will play for 80 ticks
//  Sound AQD_SB_Arc_096 will play for 80 ticks
//  Sound AQD_SB_Arc_095 will play for the defaultTick rate of 200.
//  However, because it is the last sound in the sequence it does not matter
//
// Shout out to awolcz - Play Sound Sequence. His script inspired me. Check him out at the link below.
// https://steamcommunity.com/sharedfiles/filedetails/?id=2849558644&searchtext=Play+Sound+Sequence
//
///////////////////////////////////////////////////////
// CONFIG - EDIT BELOW
///////////////////////////////////////////////////////

// float defaultTick = 200;
float defaultTick = 200;

// float delay = 10;
float delay = 10;

///////////////////////////////////////////////////////
// END CONFIG - DO NOT EDIT BELOW HERE
///////////////////////////////////////////////////////
bool isGroup;
string targetName;
string[] soundsList;
string[] sounds;
float[] timestamps;
int index;
float tick;
IMySoundBlock soundBlock;
IMyBlockGroup soundBlockGroup;
List<IMyTerminalBlock> soundBlockList = new List<IMyTerminalBlock>();

public void Main(string argument, UpdateType updateSource)
{
  // Check if playback is ready
  if (updateSource == UpdateType.Update1)
  {
    // Check if queued sounds have been played
    if (index >= sounds.Count())
    {
      ClearSound();
      Runtime.UpdateFrequency = UpdateFrequency.None;
      return;
    }
    tick++;

    // Check if enough ticks have passed, reset tick and play the next sound
    if (tick == timestamps[index])
    {
      // Check if all sounds have been played
      if (index >= sounds.Count())
      {
        ClearSound();
        Runtime.UpdateFrequency = UpdateFrequency.None;
      }
      else
      {
        if (!PrepareNextSound()) return;
      }
    }
    else if (tick == timestamps[index] + delay)
    {
      PlaySound();
    }
  }
  else if (argument != null)
  {
    // Reset index & tick
    index = 0;
    tick = 0;
    if (!PrepareSoundSequence(argument))
    {
      Echo("Invalid config string.");
      return;
    }
    if (!PrepareNextSound()) return;

    // Set program to read for playback
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
  }
}

private bool PrepareSoundSequence(string args)
{
  if (!parseArguments(args)) { Echo("Type not found."); return false; };
  if (!init()) { Echo("Issue initializing."); return false; };

  sounds = new string[soundsList.Count()];
  timestamps = new float[soundsList.Count()];
  int counter = 0;
  float totalDuration = 0;

  foreach (string sound in soundsList)
  {
    string[] values = sound.Split(':');
    // Assign sound to index equal to the current count.
    sounds[counter] = values[0];
    // Assign current timestamp.
    timestamps[counter] = totalDuration;

    // Get the duration of this sound if an overide is present.
    float duration;
    if (values.Count() >=2) duration = float.Parse(values[1]);
    else duration = defaultTick;
    totalDuration += duration;
    counter++;
  }

  return true;
}

private bool parseArguments(string args)
{
  if (!GetNameType(args.Substring(0, args.IndexOf(",")))) return false;
  SetName(args);
  GetSoundList(args.Substring(args.IndexOf(",")));
  return true;
}

private bool GetNameType(string args)
{
  string type = args.Substring(1, 1);
  if (type == "B" || type == "b") { isGroup = false; return true; }
  if (type == "G" || type == "g") { isGroup = true; return true; }
  return false;
}

private void SetName(string args) { targetName = args.Substring(3, (args.IndexOf(",") - 4)); }

private void GetSoundList(string args) { soundsList = (args.Replace(" ","")).Split(','); }

private bool init()
{
  if (!isGroup) return SetSoundBlock();
  else return SetBlockGroup();
}

private bool SetBlockGroup()
{
  IMyBlockGroup group = (IMyBlockGroup)GridTerminalSystem.GetBlockGroupWithName(targetName);
  if (group == null)
  {
    Echo("Error(SetBlockGroup): Could not set block group " + targetName);
    return false;
  }
  soundBlockGroup = group;
  return true;
}

private bool SetSoundBlock()
{
  IMySoundBlock block = (IMySoundBlock)GridTerminalSystem.GetBlockWithName(targetName);
  if (block == null)
  {
    Echo("Error(SetSoundBlock): Could not set sound block " + targetName);
    return false;
  }
  soundBlock = block;
  return true;
}

private bool PrepareNextSound()
{
  if (isGroup)
  {
    if (index >= sounds.Count()) return false;
    soundBlockGroup.GetBlocks(soundBlockList);
    foreach (var block in soundBlockList)
    {
      IMySoundBlock currentBlock = (IMySoundBlock)block;
      Echo($"Playing sound: {sounds[index]} timestamp: {timestamps[index]}");
      currentBlock.SelectedSound = sounds[index];
    }
    return true;
  }
  else
  {
    if (index >= sounds.Count()) return false;
    Echo($"Playing sound: {sounds[index]} timestamp: {timestamps[index]}");
    soundBlock.SelectedSound = sounds[index];
    return true;
  }
}

private void PlaySound()
{
  if (isGroup)
  {
    soundBlockGroup.GetBlocks(soundBlockList);
    foreach (var block in soundBlockList)
    {
      IMySoundBlock currentBlock = (IMySoundBlock)block;
      currentBlock.Play();
    }
    index++;
  }
  else
  {
    soundBlock.Play();
    index++;
  }
}

private void ClearSound()
{
  if (isGroup)
  {
    soundBlockGroup.GetBlocks(soundBlockList);
    foreach (var block in soundBlockList)
    {
      IMySoundBlock currentBlock = (IMySoundBlock)block;
      currentBlock.SelectedSound = null;
    }
    isGroup = false;
  }
  else soundBlock.SelectedSound = null;
}
