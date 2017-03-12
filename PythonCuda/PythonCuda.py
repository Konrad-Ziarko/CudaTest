import numpy as np
from timeit import default_timer as timer
from numba import cuda, vectorize
from pycuda.compiler import SourceModule

#calculate Levenshtein distance
def calcLevDist(str1, str2):
    print(str1,'\n%s'%str2)
    list1 = []
    list2 = []
    for i in str1:
        list1.append(ord(i))
    for i in str2:
        list2.append(ord(i))

    A = np.array(list1, dtype=np.int)
    B = np.array(list2, dtype=np.int)
    i = j = 0
    metric = np.zeros([A.shape[0]+1, B.shape[0]+1], dtype=np.int)

    #start = timer()
    for i in range(0, len(A)+1):
        metric[i][0] = i
    for j in range(0, len(B)+1):
        metric[0][j] = j

    for i in range(1, len(A)+1):    
        for j in range(1, len(B)+1):
            if A[i-1] == B[j-1]:
                cost = 0
            else:
                cost = 1
            metric[i][j] = min(metric[i-1][j]+1, metric[i][j-1]+1, metric[i-1][j-1] + cost)

    #vectoradd_time = timer() - start
    #print("Time:%f" % vectoradd_time)
    print(metric)
    return metric[len(A)][len(B)]
    #return metric

#calculate generalized Levenshtein distance
def calcLevGeneralization(str1, str2):
    if len(str1) == len(str2):
        cost = 0;
        for i in range(0, len(str1)):
            if str1[i] != str2[i]:
                cost=cost+1
        return cost

#find best matches in text; dist is equal to number of replace operations
def findMatches(str1, str2):
    if len(str1) >= len(str2):
        strlen = len(str2)
        offset = strlen
        minDist = len(str2)+1
        bestMatch = []
        #matchVal = []
        tmpStr = str1[0:strlen]

        for i in range (0, len(str1)-strlen+1):
            newDist = calcLevGeneralization(tmpStr, str2)
            if newDist <= minDist:
                #print(tmpStr) #print matched string
                minDist = newDist
                bestMatch.append([offset-strlen, minDist])
                #matchVal.append(minDist)

            tmpStr = tmpStr[1:]
            if offset < len(str1):
                tmpStr = tmpStr + str1[offset]
            offset = offset+1
        #print(matchVal)
        return bestMatch

def main():
    str1 = 'Wojskowa akademia techniczna im gen Jaroslawa Dąbrowskiego w warszawie'
    str2 = 'warszawie'
    
    print('[Idx, DistVal]', findMatches(str1, str2))


    with open('data.txt', 'r') as myfile:
        data=myfile.read().replace('\n', ' ')
        print('[Idx, DistVal]', findMatches(data, str2))
    
if __name__ == '__main__':
    main()