#!/bin/bash

for file in "$@"; do
	echo "${file}..."
	
	if [ -f "${file}" ]; then
		t="$(file - < "${file}" | cut -d: -f2)"
		isutf="$(echo "${t}" | grep -q 'UTF-8' && echo true || echo false)"
		withbom="$(echo "${t}" | grep -q '(with BOM)' && echo true || echo false)"
		isascii="$(echo "${t}" | grep -q 'ASCII' && echo true || echo false)"
		
		if [ "${isutf}" = "true" ] && [ "${withbom}" != "true" ] || [ "${isascii}" = "true" ]; then
			echo "+ Adding UTF-8 BOM..."
			mv "${file}"{,~}
			if uconv -f utf-8 -t utf-8 --add-signature "${file}~" -o "${file}"; then
				rm "${file}~"
			else
				echo "- Error with uconv."
				mv "${file}"{~,}
			fi
		elif [ "${isutf}" = "true" ] && [ "${withbom}" = "true" ]; then
			echo "+ Already UTF-8 BOM."
		else
			echo "- Unknown type."
		fi
	else
		echo "- Not a file."
	fi
done
