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
		elif (szExt == '.xls' or szExt == '.xlsx'):  # 编译

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
				for i in range(table.nrows):
					if i == 1:  # 忽略第二行，第二行为行头解释
						pass
					else:
						row = table.row_values(i)

						#if i == 0: # 第一行，读取Comment列位
						col = 1 # 计列
						szRow = []
						for s in row:
							if i == 0: # i = 0 时表头
								# 去掉多语言表示{xxx}
								if s.startswith("{") and s.endswith("}"):
									s = s[1:-1]
								if ("Comment" in s):
									arrIgnoreCols.append(col)
								if ("" == s): # 空白字符列
									arrIgnoreCols.append(col)
								if ("Id" in s):
									nIdCol = col

							if col in arrIgnoreCols: # 注释列忽略！
								pass
							# elif col == nIdCol and i != 0: # ID列整数！非表头
							# 	try:
							# 		toInt = int(s)
							# 		szRow.append(toInt)
							# 	except:  # 可能是字符串key,无法转int, 用原字符串吧
							# 		szRow.append(unicode(s))
							else:
								if isinstance(s, str):
									s = s.strip() # 干掉空格
								if isinstance(s, float):
									if str(s).endswith('.0'): # .0结尾的浮点，转整形
										s = int(float(s))
								szRow.append(unicode(s))

							col = col + 1

						_new_line = '\t'.join(unicode(s) for s in szRow)
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