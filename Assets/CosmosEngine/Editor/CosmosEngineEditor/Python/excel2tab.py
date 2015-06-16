#coding=utf-8
#!py.exe

reload(sys)   
sys.setdefaultencoding('utf8')
cur_script_path = os.path.split(os.path.realpath(__file__))[0]
sys.path.insert(0, cur_script_path + '/python_excel_lib.zip')  # Add .zip file to front of path, include~ xlrd, xlwt
# os.chdir(cur_script_path)

import os
from os import path
import xlrd
import time
import sys 
import shutil
import re

# print 'ok'
# output_tab = None




DIRS = ["setting/", "server_setting/", "editor_setting/"]
INPUT_PATH = "../Scheme/"  # 需要设置！
OUTPUT_PATH = "../Product/"
EXPORT_EXT = "bytes"


def DirWalker(arg, dirname, filenames):
	print dirname
	# if not os.path.exists(INPUT_PATH):
	# 	os.mkdir(INPUT_PATH)
	# if not os.path.exists(OUTPUT_PATH):
	# 	os.mkdir(OUTPUT_PATH)

	# print arg, dirname, filenames

	# mm_files = [ f for f in os.listdir(INPUT_PATH) \
	# 	if path.isfile(path.join(INPUT_PATH,f)) and \
	# 	(f[-4:] == '.xls' or f[-5:] == '.xlsx')] # mm扩展名的文件

	for mm_file in filenames:
		szExt = os.path.splitext(mm_file)[1]

		if mm_file.startswith("comment"):   # 注释excel文件忽略！
			continue

		if not os.path.exists(dirname):
			os.mkdir(dirname)

		# 原地址
		szSrcPath = '%s/%s' % (dirname, mm_file) 

		# 输出地址
		# 当前目录+分隔符+输出目录+文件子目录（替换掉输入目录)
		szExportDir = "%s%s%s%s" % (os.getcwd(), os.sep, OUTPUT_PATH, dirname.replace(INPUT_PATH, ""))
		szExportPath = "%s/%s.%s" %(szExportDir, os.path.splitext(mm_file)[0], EXPORT_EXT)
		
		if not os.path.exists(szExportDir):
			os.mkdir(szExportDir)

		if (szExt == '.bytes'): # 纯拷贝
			shutil.copy(szSrcPath, szExportPath)
			print u'【Copied Pure Text File】 From %s To %s' % (szSrcPath.decode('gbk').encode('utf-8'), szExportPath.decode('gbk').encode('utf-8'))
		elif (szExt == '.xls' or szExt == '.xlsx'):  # 下面开始编译~

			str_list = []
			
			print u'【Compiling Excel】: %s' % szSrcPath.decode('gbk').encode('utf-8')

			data = xlrd.open_workbook(szSrcPath)
			table = data.sheet_by_index(0) #通过索引顺序获取
			if table == None:
				print u'【Error】: Not Found File %s' % szSrcPath
			else:
				# 找到表
				arrIgnoreCols = [] # 忽略的列
				nIdCol = None  # ID列索引编号
				
				bCommentRow = 1 # 默认注释行是第二行，如果第二行是带[xxx]，则注释行改成第3行

				rePattern = re.compile('\[(.*)\]')

				for rowIndex in range(table.nrows):

					row = table.row_values(rowIndex)

					# 行的第一个格以#开头，忽略整行！
					if rowIndex != 1 and len(row) > 0 and str(row[0]).startswith("#"):
						continue

					if rowIndex == 1:  
						# 判断是否是声明行~如果是就编译这样，忽略下一行
						bHasDefRow = False # 是否拥有声明行
						for cell_ in row:
							if rePattern.search(str(cell_)):
								bCommentRow = 2
								bHasDefRow = True
								print u'[带有定义行]: %s' % szExportPath.decode('gbk').encode('utf-8')
								break
						if not bHasDefRow:
							# 不拥有声明行,强制添加空行
							str_list.append('[string]')



					# 忽略行头解释
					if rowIndex == bCommentRow:
						continue


					#if rowIndex == 0: # 第一行，读取Comment列位
					col = 0 # 计列
					szRow = []
					for cell_ in row:

						cellVal = cell_
						if rowIndex == 0: # rowIndex = 0 时表头
							if (cellVal.startswith("#")):
								arrIgnoreCols.append(col)
							if ("Comment" in cellVal):
								arrIgnoreCols.append(col)
							if ("" == cellVal): # 空白字符列
								arrIgnoreCols.append(col)
							if ("Id" in cellVal):
								nIdCol = col

							# 去掉多语言表示{xxx}
							if cellVal.startswith("{") and cellVal.endswith("}"):
								cellVal = cellVal[1:-1]
								# szType = 'l10n'
							"""
							commentRow = table.row_values(1) # 获取第二行，注释行
							
							commentCell = commentRow[col]
							# 获取特殊属性声明列~[string:abc]  类型和默认值
							reSearchComment = rePattern.search(commentCell, re.MULTILINE)
							szType = None
							szDefault = ''
							if (reSearchComment): # 搜索注释列，带有类型声明
								splitMatch = reSearchComment.group(1).split(':')
								szType = splitMatch[0]
								if len(splitMatch) > 1:
									szDefault = splitMatch[1]


							reSearchHeader = rePattern.search(cellVal, re.MULTILINE) # 表头声明解析
							if (reSearchHeader):
								splitMatch = reSearchHeader.group(1).split(':')
								szType = splitMatch[0]
								if len(splitMatch) > 1:
									szDefault = splitMatch[1]
								cellVal = cellVal.replace(reSearchHeader.group(0), "") # 替换掉匹配的

							if (szType): # 带有类型，那么作处理
								cellVal = '%s|%s|%s' % (cellVal, szType, szDefault)
							"""

						if col in arrIgnoreCols: # 注释列忽略！
							pass
						# elif col == nIdCol and rowIndex != 0: # ID列整数！非表头
						# 	try:
						# 		toInt = int(cellVal)
						# 		szRow.append(toInt)
						# 	except:  # 可能是字符串key,无法转int, 用原字符串吧
						# 		szRow.append(unicode(cellVal))
						else:
							if isinstance(cellVal, str) or isinstance(cellVal, unicode):
								cellVal = cellVal.strip() # 干掉空格
								cellVal = cellVal.replace('\n', '').replace('\r', '') # 确保去掉换行符

							if isinstance(cellVal, float):
								if str(cellVal).endswith('.0'): # .0结尾的浮点，转整形
									cellVal = int(float(cellVal))
							szRow.append(unicode(cellVal))

						col = col + 1

					_new_line = '\t'.join(unicode(cellVal) for cellVal in szRow)
					# print _new_line
					if _new_line.strip() != '':
						str_list.append(_new_line)

				strOutput = '\n'.join(str_list).encode('utf-8')
				
				file_w = open(szExportPath, 'w')
				file_w.write(strOutput)

				print u'【Compiled Success Tab File】: %s' % szExportPath.decode('gbk').encode('utf-8')


if __name__ == '__main__':
	INPUT_PATH = sys.argv[1]
	OUTPUT_PATH = sys.argv[2]

	for dir in DIRS:
		os.path.walk(INPUT_PATH + dir, DirWalker, ())

	print u'【All OK!】Wait 3s to Close.....'
	time.sleep(3)