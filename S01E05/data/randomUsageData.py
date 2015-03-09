import csv
import random

with open('random-usage.txt', 'w', newline='\n') as csvfile:
    csvwriter = csv.writer(csvfile, delimiter=',', quoting=csv.QUOTE_MINIMAL)
    for x in range(1,500):
        userId=random.randrange(1,13)
        itemId=random.randrange(1,12)
        csvwriter.writerow([str(userId), str(itemId)])
