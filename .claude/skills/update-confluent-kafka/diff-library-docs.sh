#!/usr/bin/env bash
# Lists the documented properties of the proxied Confluent config classes that were added, removed or whose <summary> changed between two
# library versions, each with Silverback's current summary (from the generator's DocumentationProvider.cs, "none" if it has no entry).
# Whitespace is normalized, so reflowed text is not reported. A version missing from the NuGet global packages folder (or from $NUGET_PACKAGES)
# is downloaded from nuget.org into a temp folder that is deleted on exit; only the XML docs are extracted and read.
# Usage (from the repository root, Git Bash): .claude/skills/update-confluent-kafka/diff-library-docs.sh <old-version> <new-version>
set -euo pipefail

old_version=${1:?Usage: diff-library-docs.sh <old-version> <new-version>}
new_version=${2:?Usage: diff-library-docs.sh <old-version> <new-version>}

provider=tools/Silverback.Tools.Generators.KafkaConfigProxies/DocumentationProvider.cs
# Must match the config classes proxied by the generator (Program.cs) and the DocumentationProvider.AppendSummary switch
packages="Confluent.Kafka Confluent.SchemaRegistry Confluent.SchemaRegistry.Serdes.Avro Confluent.SchemaRegistry.Serdes.Json Confluent.SchemaRegistry.Serdes.Protobuf"
classes='^(Config|ClientConfig|ConsumerConfig|ProducerConfig|SchemaRegistryConfig|SerdeConfig|(Avro|Json|Protobuf)(Serializer|Deserializer)Config)\.[A-Za-z0-9_]+$'

root=${NUGET_PACKAGES:-$(dotnet nuget locals global-packages -l | tr -d '\r' | sed 's/^[^:]*: //')}
if command -v cygpath > /dev/null; then root=$(cygpath -u "$root"); fi

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

# Prints the path of the XML docs of a package version (highest net<major>.<minor> target framework, else the highest netstandard*),
# downloading the package if no copy with XML docs exists. Prints an error and fails if that is not possible.
xml_path() {
    local id=$1 version=$2 lower_id=${1,,} lower_version=${2,,} dir files file
    dir=$root/$lower_id/$lower_version

    if ! compgen -G "$dir/lib/*/$id.xml" > /dev/null; then
        dir=$tmp/$lower_id.$lower_version
        if ! compgen -G "$dir/lib/*/$id.xml" > /dev/null; then
            mkdir -p "$dir"
            curl -fsSL -o "$tmp/pkg.nupkg" "https://api.nuget.org/v3-flatcontainer/$lower_id/$lower_version/$lower_id.$lower_version.nupkg" \
                || { echo "Cannot download $id $version from nuget.org (does the version exist?)" >&2; return 1; }
            unzip -q -o "$tmp/pkg.nupkg" "lib/*/$id.xml" -d "$dir" \
                || { echo "No XML docs found in the $id $version package" >&2; return 1; }
            rm -f "$tmp/pkg.nupkg"
        fi
    fi

    files=$(ls "$dir"/lib/*/"$id.xml")
    file=$(echo "$files" | { grep -E '/net[0-9]+\.[0-9]+/' || true; } | sort -V | tail -1)
    [[ -n $file ]] || file=$(echo "$files" | { grep -E '/netstandard' || true; } | sort -V | tail -1)
    [[ -n $file ]] || { echo "No XML docs found for $id $version" >&2; return 1; }
    echo "$file"
}

# Prints "Class.Property<TAB>normalized summary" for the proxied classes of a library version
members() {
    local version=$1 id file
    for id in $packages; do
        file=$(xml_path "$id" "$version") || exit 1
        tr -d '\r' < "$file" | classes="$classes" awk '
            /<member name="P:/ {
                name = $0; sub(/.*name="P:/, "", name); sub(/".*/, "", name)
                sub(/^Confluent\.(Kafka|SchemaRegistry(\.Serdes)?)\./, "", name)
                keep = name ~ ENVIRON["classes"]; text = ""
            }
            keep { text = text " " $0 }
            keep && /<\/member>/ {
                if (text ~ /<summary>/) { sub(/.*<summary>/, "", text); sub(/<\/summary>.*/, "", text) } else { text = "" }
                gsub(/<!\[CDATA\[|\]\]>/, "", text); gsub(/[ \t]+/, " ", text); gsub(/^ | $/, "", text)
                print name "\t" text
                keep = 0
            }'
    done | LC_ALL=C sort -t "$(printf '\t')" -k1,1
}

# Prints "Section.Property<TAB>summary" for each entry of the DocumentationProvider switches (the sections are named after the config classes)
silverback() {
    tr -d '\r' < "$provider" | awk '
        /private static void AppendSummaryFor/ { section = $0; sub(/.*AppendSummaryFor/, "", section); sub(/\(.*/, "", section) }
        # A case guarded by "when property.DeclaringType == typeof(X)" is keyed by that class (X.Property), the others by the section
        /case nameof\([A-Za-z]+\.[A-Za-z0-9]+\)/ {
            key = $0; sub(/.*nameof\([A-Za-z]+\./, "", key); sub(/\).*/, "", key); text = ""
            if ($0 ~ /when property\.DeclaringType == typeof\(/) { guard = $0; sub(/.*typeof\(/, "", guard); sub(/\).*/, "", guard); key = guard "." key }
            else key = section "." key
        }
        /builder\.AppendLine\("    \/\/\// && key != "" {
            line = $0; sub(/.*AppendLine\("[ ]*\/\/\/ */, "", line); sub(/"\);[ ]*$/, "", line); gsub(/\\"/, "\"", line)
            text = text " " line
        }
        /break;/ && key != "" {
            gsub(/[ \t]+/, " ", text); gsub(/^ | $/, "", text)
            summaries[key] = (key in summaries) ? summaries[key] " || " text : text
            key = ""
        }
        END { for (k in summaries) print k "\t" summaries[k] }'
}

members "$old_version" > "$tmp/old.tsv"
members "$new_version" > "$tmp/new.tsv"
silverback > "$tmp/silverback.tsv"
cut -f1 "$tmp/old.tsv" "$tmp/new.tsv" | LC_ALL=C sort -u > "$tmp/keys.txt"

echo "# Library docs $old_version -> $new_version (silverback: current summary in $provider)"

awk -F '\t' '
    # Shows only the differing middle part of two texts, widened to whole words
    function delta(a, b,    p, s, la, lb) {
        la = length(a); lb = length(b)
        for (p = 0; p < la && p < lb && substr(a, p + 1, 1) == substr(b, p + 1, 1); p++);
        while (p > 0 && substr(a, p, 1) != " ") p--
        for (s = 0; s < la - p && s < lb - p && substr(a, la - s, 1) == substr(b, lb - s, 1); s++);
        while (s > 0 && substr(a, la - s + 1, 1) != " ") s--
        return "-[" substr(a, p + 1, la - p - s) "] +[" substr(b, p + 1, lb - p - s) "]"
    }
    FILENAME == ARGV[1] { old[$1] = $2; next }
    FILENAME == ARGV[2] { new[$1] = $2; next }
    FILENAME == ARGV[3] { silverback[$1] = $2; next }
    {
        key = $1; class = key; sub(/\..*/, "", class); sub(/^(Avro|Json|Protobuf)/, "", class)
        property = key; sub(/^[^.]*\./, "", property)
        fullclass = key; sub(/\..*/, "", fullclass)
        if ((fullclass "." property) in silverback) sb = silverback[fullclass "." property]
        else if ((class "." property) in silverback) sb = silverback[class "." property]
        else sb = "none"

        if (!(key in old))        { print "ADDED " key; print "  new: " new[key] }
        else if (!(key in new))   { print "REMOVED " key; print "  old: " old[key] }
        else if (old[key] != new[key]) { print "CHANGED " key; print "  delta: " delta(old[key], new[key]); print "  old: " old[key]; print "  new: " new[key] }
        else next
        print "  silverback: " sb
        count++
    }
    END { if (!count) print "No differences." }' "$tmp/old.tsv" "$tmp/new.tsv" "$tmp/silverback.tsv" "$tmp/keys.txt"
