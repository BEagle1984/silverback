---
layout: splash
permalink: /
header:
  overlay_color: "#5e616c"
  overlay_image: /assets/images/splash.jpg
  actions:
    - label: "<i class='fab fa-github'></i> View on GitHub"
      url: "https://github.com/BEagle1984/silverback/"
    - label: "<i class='fas fa-arrow-alt-circle-down'></i> Get from NuGet"
      url: "https://www.nuget.org/packages?q=Silverback"
excerpt: Silverback is a simple framework to build reactive, event-driven, microservices with .net core.
features:
  - title: "Simple yet Powerful"
    excerpt: "A very simple and not too invasive framework with just enough features to cover most of the real world use cases when it comes to messaging and microservices integration."
    url: "/docs/features"
    btn_label: "View Features"
    btn_class: "btn--primary"
  - title: "Flexible and Extensible"
    excerpt: "It is designed to be highly configurable and extensible to cover as many use cases as possible."
    url: "/docs"
    btn_label: "View Documentation"
    btn_class: "btn--primary"
  - title: "Open Source"
    excerpt: "Silverback is released under MIT license and is completely free and opensource."
    url: "https://github.com/BEagle1984/silverback/"
    btn_label: "View Source Code"
    btn_class: "btn--primary"
---

{% include feature_row id="intro" type="center" %}
{% include feature_row id="features" %}

# Latest Posts

{% for post in site.posts limit:3 %}
  {% include archive-single.html %}
{% endfor %}