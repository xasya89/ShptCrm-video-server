SELECT * FROM ent.actshpt_files;CREATE TABLE `actshpt_files` (
  `id` int NOT NULL AUTO_INCREMENT,
  `ActId` int NOT NULL,
  `FileName` varchar(200) NOT NULL,
  `Processed` tinyint(1) DEFAULT NULL,
  `DevId` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=32 DEFAULT CHARSET=utf8mb3;


CREATE TABLE `actshpt_video` (
  `id` int NOT NULL AUTO_INCREMENT,
  `ActId` int NOT NULL,
  `Start` datetime NOT NULL,
  `Stop` datetime DEFAULT NULL,
  `Note` text,
  `DevId` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb3;
