const packages = [
    'Silverback.Integration.Kafka',
    'Silverback.Integration.MQTT',
    'Silverback.Core',
    'Silverback.Core.Model',
    'Silverback.Core.Rx',
    'Silverback.Integration',
    'Silverback.Integration.Kafka.SchemaRegistry',
    'Silverback.Integration.Newtonsoft',
    'Silverback.Integration.HealthChecks',
    'Silverback.Integration.Testing',
    'Silverback.Integration.Kafka.Testing',
    'Silverback.Integration.MQTT.Testing',
    'Silverback.Storage.EntityFramework',
    'Silverback.Storage.PostgreSql',
    'Silverback.Storage.Sqlite',
    'Silverback.Storage.Memory',
    'Silverback.Storage.Relational'
]

// Cycles the package name shown in the landing page install command
function rotateInstallPackage() {
    const element = document.querySelector('.sb-install .sb-package')

    if (!element || window.matchMedia('(prefers-reduced-motion: reduce)').matches)
        return

    let index = 0

    setInterval(() => {
        element.classList.add('sb-package-out')

        setTimeout(() => {
            index = (index + 1) % packages.length
            element.textContent = packages[index]
            element.classList.remove('sb-package-out')
        }, 300)
    }, 2500)
}

export default {
    defaultTheme: 'dark',
    iconLinks: [
        {
            icon: 'github',
            href: 'https://github.com/BEagle1984/silverback/',
            title: 'View on GitHub'
        },
        {
            icon: 'box-seam',
            href: 'https://www.nuget.org/packages?q=Silverback',
            title: 'NuGet packages'
        }
    ],
    start: rotateInstallPackage
}
