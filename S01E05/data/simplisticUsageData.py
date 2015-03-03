import csv
import random

def addUsage(csvwriter, itemId, nbRows):
    for y in range(0, nbRows):
        userId=random.randrange(1,13)
        csvwriter.writerow([str(userId), str(itemId)])

with open('simplistic-usage.txt', 'w', newline='\n') as csvfile:
    csvwriter = csv.writer(csvfile, delimiter=',', quoting=csv.QUOTE_MINIMAL)

    # les chats consomment surtout 1, puis 2, puis 3, etc...
    for x in range(1,6):
        addUsage(csvwriter, x, (6-x)*10)

    # les chats consomment surtout 8 et 10, un peu de 9 et 11
    addUsage(csvwriter,  8, 15)
    addUsage(csvwriter, 10, 15)
    addUsage(csvwriter,  9, 5)
    addUsage(csvwriter, 11, 5)
