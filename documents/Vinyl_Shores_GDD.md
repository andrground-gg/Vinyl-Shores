# Vinyl Shores

## Game Design Document

# Table of Contents

1.  Game Overview --- 2
2.  Vision statement --- 2
3.  Gameplay --- 3
4.  Story --- 4
5.  World --- 5
6.  Media List --- 6
7.  Technical specification (if applicable) --- 7

# 1. Game Overview

Vinyl Shores is a small top-down 2D pixel-art exploration and rhythm
game. A young musician and record collector arrives on an island looking
for inspiration and helps an elderly musician recover his lost album by
finding nine records and completing musical challenges.

The island has three areas: beach, forest, and mountain, with three
records in each. After collecting all nine, the player faces a final
boss and completes the story.

# 2. Vision Statement

Create a simple, approachable island adventure where rhythm gameplay is
the central feature. Exploration and short conversations provide context
for each song, while existing pixel-art assets keep the visual scope
manageable.

# 3. Gameplay

**Core loop:** In each area, meet an introductory NPC who wanders within
a small, easy-to-find area. Talk to them for a verbal clue, explore on
foot, find the record's guardian, and talk to them. Complete their
rhythm challenge to earn the record and hear a short line of dialogue;
retry if unsuccessful. Return to the introductory NPC for the next clue.
After three records, proceed to the next area; in the final area, unlock
the final boss.

The first introductory NPC can be the old musician at his beach cabin;
the others can be island inhabitants who know his music. Dialogue can be
repeated so clues can be reread. Records within an area are found in
sequence.

**Rhythm gameplay:** Horizontal lanes display notes travelling toward a
fixed hit marker. Press the indicated key when a note reaches the
marker; provide immediate timing feedback. The travel direction, layout,
and whether keys are fixed per lane or vary remain to be settled in the
prototype; fixed keys are the simplest recommended option.

The beach uses two lanes and tap notes; the forest introduces a third
lane and hold notes; the mountain introduces a fourth lane, faster or
denser patterns, and a final combination of learned skills. Tap notes
require one press; hold notes require a press at the start and release
at the end, with early-release feedback to be defined. Simultaneous
notes are an option after the basic prototype works.

**Results:** Below 50% fails and requires a retry; 50% or more succeeds
and awards the record. Show the percentage and one to three stars on
success. Suggested thresholds are 50% to below 70% for one star, 70% to
below 90% for two, and 90% to 100% for three. A failed attempt grants no
record and offers an immediate retry.

Stars affect only the final character's dialogue after defeat; they do
not unlock areas, records, mechanics, or another ending. Keep the best
stars for each of the nine record challenges, sum them out of 27, and
choose one of three final dialogue lines using thresholds agreed by the
team. The boss is excluded from this total, but must be passed. Replay
can improve a best result and is never required for progression.

Define timing windows, partial credit, hold scoring, missed notes, and
maximum points during prototyping. Score percentage is earned points
divided by maximum possible points. Show walking and interaction
controls, rhythm key labels, the hit point, readable notes, timing
feedback, score and result screen. Provide pause and retry.

# 4. Story

The elderly musician once came to the island seeking inspiration and
composed an album from his experiences there. He has lost its records
and is discouraged. The protagonist agrees to recover them in exchange
for the musician's guidance. His memories and the islanders'
conversations reveal the story of each piece. Completing challenges lets
the protagonist engage with the music.

Islanders have kept the scattered records safe and invite the
protagonist to play the music before handing them over. After the ninth
record, the protagonist confronts the final character in a rhythm
challenge. The album is reunited, and the old musician acknowledges the
protagonist's progress and new inspiration. A future collaboration on a
new album is an optional narrative idea outside the playable scope.

# 5. World

**Beach --- Three records.** The starting area has the old man's cabin,
a small dock, a warm sunset, waves and an approachable layout. Candidate
content includes the tutorial and first track near the cabin, a sandy
location, and a beach or harbor character. Lo-fi, reggae or other
relaxed music could suit the area.

**Forest --- Three records.** Dense vegetation and paths; choose the
look supported by the pack. Candidate locations include a tree area,
stream and small hut, with folk, jazz or percussion-led music. Hold
notes are introduced here.

**Mountain--- Three records.** An upward route with cliffs and bridges,
using the closest available tiles. Candidate locations include the
foothills, a cave or rocky path, and the summit; rhythm patterns become
more challenging. The musical genre is undecided.

These are location ideas, not commitments to climbing, swinging,
reconstruction puzzles or a maze. All records are earned from guardians
through rhythm challenges. Use existing sprites; a surfer, musician,
owl, forest spirit, monk and DJ are possible inspirations, subject to
the pack. The first beach record is earned from its guardian after the
tutorial. The final boss is a separate tenth challenge and does not
award a tenth record; this interpretation can be changed by the team.

Gates or paths open after all three records in an area are collected,
checking progress without consuming records. The cabin may contain a
record player to replay completed songs; if implemented, the player must
be able to access it.

## 5.1 Characters

**The protagonist (working name: Digger)** is a young musician and
record collector who comes to the island seeking inspiration. Headphones
and a backpack of vinyl are visual ideas, subject to the sprites
available in the asset pack.

**The old musician (working name: Old Spin)** is the elderly owner of
the beach cabin and the composer of the lost album. He serves as the
story's central guide and may be the beach's introductory NPC. He shares
memories of the pieces and gives clues to their locations. His sprite
should be easy to recognize.

**Introductory NPCs:** Each area has an approachable character who walks
within a small, easy-to-find area. They give the player a verbal hint
for each next record and repeat the hint on request. The forest and
mountain guides may be islanders familiar with the old musician and his
work.

**Record guardians:** Three characters in each area safeguard the
records. Each offers a short dialogue before and after a rhythm
challenge and gives the player their record on success. Earlier
character concepts can guide selection from the pack: a surfer, beach
musician or harbor captain for the beach; a wandering musician, jazz owl
or forest spirit for the forest; and a monk or other suitable mountain
inhabitants for the mountain. Specific appearances and musical
associations depend on available sprites and the team's choices.

**Final character:** A summit DJ was suggested as a final boss. After
all nine records are recovered, the player faces this character in a
separate rhythm challenge. Their final dialogue changes according to the
player's accumulated stars. The boss does not give a tenth record.

**Visual approach:** Use existing Ninja Adventure sprites and, where
useful, existing portraits. The player needs movement animations; NPCs
can use simple idle or walking animations. Recoloring or small
adaptations are optional. Larger custom battle sprites, hit animations
and defeat animations are not required.

# 6. Media List

Use the Ninja Adventure asset pack as the source for visual assets,
aiming for no external graphics. Select existing sprites and tiles
before committing to specific characters or environments. Recolor or
minimally adapt pack assets only if the team chooses; use existing
portraits or in-game sprites for dialogue and rhythm screens. No larger
custom battle sprites or special defeat animations are required. Basic
lanes, notes and the score display can be simple UI elements.

Asset pack: https://pixel-boy.itch.io/ninja-adventure-asset-pack. The
page lists characters, tiles, UI elements, sound effects and music and
describes the pack as CC0. Check the downloaded files for suitable
environments, characters and record-like items. A record can be
represented by existing UI, text or a suitable pack object if there is
no literal vinyl sprite.

Music will be made by a team member.

# 7. Technical specification

Game engine: Unity 6000.3.12f1.

The game uses top-down 2D pixel art and a rhythm interface with
horizontal note lanes. The precise rhythm layout, lane-to-key mapping
and scoring details will be settled during prototyping.

# 8. Team
1. Oriol Parent
2. Andres Bernardini
3. Aitor Fernandez
4. Vadim Andreev
5. Shalin Rahel
6. Rodrigo Rojas
