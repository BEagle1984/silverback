# How to contribute

Please read these guidelines before contributing to Silverback:

* [Issues and Bugs](#issue)
* [Feature Requests](#feature)
* [Submitting a Pull Request](#pullrequest)
* [Contributor License Agreement](#cla)

## <a name="issue"></a> Found an Issue?

If you find a bug in the source code or a mistake in the documentation, you can help by
submitting an issue to the [GitHub repository][github]. Or, even better, you can submit a [pull request](#pullrequest) with a fix.

When submitting an issue please include the following information:

* A description of the issue
* The exception message and stacktrace if an error was thrown
* If possible, please include code that reproduces the issue. [DropBox][dropbox] or GitHub's [Gist][gist] can be used to share large code samples, or you could [submit a pull request](#pullrequest) with the issue reproduced in a new unit test.

The more information you include about the issue, the more likely it is to be fixed!

## <a name="feature"></a> Want a Feature?

You can request a new feature by submitting an issue to the [GitHub repository][github]. Before requesting a feature consider the following:
* Silverback has many extensibility points, it is very likely that you can implement your feature without having to modify Silverback
* Stability is important and large breaking changes are unlikely to be accepted

## <a name="pullrequest"></a> Submitting a Pull Request

When submitting a pull request to the [GitHub repository][github] make sure to do the following:
* Check that new and updated code follows Silverback's existing code formatting and naming standard
* Run all unit tests to ensure no existing functionality has been affected
* Write new unit tests to test your changes: all features and fixed bugs must have tests to verify they work

Read [GitHub help][pullrequesthelp] for more details about creating pull requests.

Detailed step-by-step instructions to build and test Silverback can be found in the [project's website][build-docs].

## <a name="cla"></a> Contributor License Agreement

By contributing your code to Silverbvack you grant Sergio Aquilini a non-exclusive, irrevocable, worldwide, royalty-free, sublicenseable, transferable license under all of Your relevant intellectual property rights (including copyright, patent, and any other rights), to use, copy, prepare derivative works of, distribute and publicly perform and display the Contributions on any licensing terms, including without limitation: (a) open source licenses like the MIT license; and (b) binary, proprietary, or commercial licenses. Except for the licenses granted herein, You reserve all right, title, and interest in and to the Contribution.

You confirm that you are able to grant us these rights. You represent that You are legally entitled to grant the above license. If Your employer has rights to intellectual property that You create, You represent that You have received permission to make the Contributions on behalf of that employer, or that Your employer has waived such rights for the Contributions.

You represent that the Contributions are Your original works of authorship, and to Your knowledge, no other person claims, or has the right to claim, any right in any invention or patent related to the Contributions. You also represent that You are not legally obligated, whether by entering into an agreement or otherwise, in any way that conflicts with the terms of this license.

Sergio Aquilini acknowledges that, except as explicitly described in this Agreement, any Contribution which
you provide is on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
INCLUDING, WITHOUT LIMITATION, ANY WARRANTIES OR CONDITIONS OF TITLE, NON-INFRINGEMENT, MERCHANTABILITY, OR FITNESS FOR A PARTICULAR PURPOSE.


[github]: https://github.com/BEagle1984/silverback
[dropbox]: https://www.dropbox.com
[gist]: https://gist.github.com
[pullrequesthelp]: https://help.github.com/articles/using-pull-requests
[build-docs]: https://beagle1984.github.io/silverback/docs/source/contributing
